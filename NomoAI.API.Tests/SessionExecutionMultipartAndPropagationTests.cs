using System.Text.Json;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Infrastructure.Ai;

namespace NomoAI.API.Tests;

public class SessionExecutionMultipartAndPropagationTests
{
    [Fact]
    public async Task First_Evaluate_Request_Omits_SessionExecution()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        using var content = AiCoreClient.BuildMultipartContent(new AiEvaluateAttemptV2Request
        {
            AudioStream = stream,
            FileName = "a.webm",
            ContentType = "audio/webm",
            ActivityType = "word",
            Prompt = "بابا",
            SpeechLevel = "vocalization",
            Age = 6,
            AttemptNumber = 1
        });

        var names = content
            .Select(c => c.Headers.ContentDisposition?.Name?.Trim('"'))
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("sessionExecution", names);
    }

    [Fact]
    public async Task FollowUp_Evaluate_Request_Sends_Previous_SessionExecution_Json()
    {
        var execution = new AiSessionExecutionDto
        {
            OriginalTarget = "بابا",
            PracticeTarget = "با با",
            CurrentStep = 2,
            AttemptCount = 3,
            HintsUsed = 2,
            SimplificationLevel = 1,
            PreviousActions = ["retry_same", "retry_with_hint", "simplify"],
            Phase = "wait_for_child",
            ModeledCurrentTarget = false,
            SlowedDown = false
        };

        await using var stream = new MemoryStream([1, 2, 3]);
        using var content = AiCoreClient.BuildMultipartContent(new AiEvaluateAttemptV2Request
        {
            AudioStream = stream,
            FileName = "a.webm",
            ContentType = "audio/webm",
            ActivityType = "word",
            Prompt = "بابا",
            SpeechLevel = "vocalization",
            Age = 6,
            AttemptNumber = 4,
            PreviousDecision = "simplify",
            SessionExecution = execution
        });

        HttpContent executionPart = content.First(c =>
            c.Headers.ContentDisposition?.Name?.Trim('"') == "sessionExecution");
        string raw = await executionPart.ReadAsStringAsync();
        AiSessionExecutionDto? parsed = JsonSerializer.Deserialize<AiSessionExecutionDto>(
            raw,
            AiCoreJsonSerializerOptions.Instance);

        Assert.NotNull(parsed);
        Assert.Equal("بابا", parsed!.OriginalTarget);
        Assert.Equal("با با", parsed.PracticeTarget);
        Assert.Equal(2, parsed.HintsUsed);
        Assert.Equal(1, parsed.SimplificationLevel);
        Assert.Equal("wait_for_child", parsed.Phase);

        HttpContent promptPart = content.First(c =>
            c.Headers.ContentDisposition?.Name?.Trim('"') == "prompt");
        Assert.Equal("بابا", await promptPart.ReadAsStringAsync());
    }

    [Fact]
    public void End_To_End_Attempt_Chain_Carries_Latest_Execution_State()
    {
        AiSessionExecutionDto? carried = null;
        string doctorPrompt = "بابا";

        carried = SimulateRound(
            carried,
            doctorPrompt,
            action: "retry_same",
            practiceTarget: "بابا",
            hintsUsed: 0,
            simplificationLevel: 0);

        Assert.NotNull(carried);
        Assert.Equal("بابا", carried!.OriginalTarget);
        Assert.Equal(0, carried.HintsUsed);

        carried = SimulateRound(
            carried,
            doctorPrompt,
            action: "retry_with_hint",
            practiceTarget: "بابا",
            hintsUsed: 1,
            simplificationLevel: 0);

        Assert.Equal(1, carried.HintsUsed);
        Assert.Equal("wait_for_child", carried.Phase);

        carried = SimulateRound(
            carried,
            doctorPrompt,
            action: "simplify",
            practiceTarget: "با با",
            hintsUsed: 1,
            simplificationLevel: 1);

        Assert.Equal("با با", carried.PracticeTarget);
        Assert.Equal(1, carried.SimplificationLevel);
        Assert.Equal("بابا", carried.OriginalTarget);

        carried = SimulateRound(
            carried,
            doctorPrompt,
            action: "model",
            practiceTarget: "بابا",
            hintsUsed: 1,
            simplificationLevel: 0,
            modeled: true);

        Assert.Equal("بابا", carried.PracticeTarget);
        Assert.True(carried.ModeledCurrentTarget);

        carried = SimulateRound(
            carried,
            doctorPrompt,
            action: "advance",
            practiceTarget: "بابا",
            hintsUsed: 1,
            simplificationLevel: 0,
            modeled: true);

        Assert.Equal("advance", carried.PreviousActions[^1]);
        Assert.Equal("بابا", doctorPrompt);
        Assert.Equal("بابا", carried.OriginalTarget);
    }

    private static AiSessionExecutionDto SimulateRound(
        AiSessionExecutionDto? previous,
        string doctorPrompt,
        string action,
        string practiceTarget,
        int hintsUsed,
        int simplificationLevel,
        bool modeled = false)
    {
        var request = new AiEvaluateAttemptV2Request
        {
            AudioStream = Stream.Null,
            FileName = "a.webm",
            ContentType = "audio/webm",
            ActivityType = "word",
            Prompt = doctorPrompt,
            SpeechLevel = "vocalization",
            Age = 6,
            AttemptNumber = (previous?.AttemptCount ?? 0) + 1,
            SessionExecution = previous
        };

        Assert.Equal(doctorPrompt, request.Prompt);
        if (previous is not null)
        {
            Assert.Same(previous, request.SessionExecution);
        }
        else
        {
            Assert.Null(request.SessionExecution);
        }

        var nextActions = new List<string>(previous?.PreviousActions ?? []);
        nextActions.Add(action);

        return new AiSessionExecutionDto
        {
            OriginalTarget = doctorPrompt,
            PracticeTarget = practiceTarget,
            AttemptCount = request.AttemptNumber,
            HintsUsed = hintsUsed,
            SimplificationLevel = simplificationLevel,
            PreviousActions = nextActions,
            Phase = "wait_for_child",
            ModeledCurrentTarget = modeled,
            SlowedDown = action == "slow_down"
        };
    }
}
