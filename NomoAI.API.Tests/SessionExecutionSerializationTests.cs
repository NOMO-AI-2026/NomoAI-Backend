using System.Text.Json;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Features.Sessions.Runtime;
using NomoAI.API.Infrastructure.Ai;

namespace NomoAI.API.Tests;

public class SessionExecutionSerializationTests
{
    [Theory]
    [InlineData("advance")]
    [InlineData("retry_same")]
    [InlineData("retry_with_hint")]
    [InlineData("simplify")]
    [InlineData("model")]
    [InlineData("slow_down")]
    [InlineData("take_short_break")]
    [InlineData("end_attempts")]
    [InlineData("recommend_doctor_review")]
    [InlineData("ask_follow_up")]
    [InlineData("continue_conversation")]
    [InlineData("end")]
    public void Adaptive_Action_Strings_Roundtrip_Without_Enum_Collision(string action)
    {
        var decision = new AiAdaptiveDecisionDto
        {
            Action = action,
            ReasonCodes = ["first_attempt"],
            AttemptNumber = 1,
            ScoreTrend = "not_available",
            RecommendedDifficulty = "same",
            OriginalTarget = "بابا",
            PracticeTarget = "با با",
            SimplificationLevel = 1,
            SessionPhase = "wait_for_child",
            HintsUsed = 1
        };

        string json = JsonSerializer.Serialize(decision, AiCoreJsonSerializerOptions.Instance);
        AiAdaptiveDecisionDto? roundTrip = JsonSerializer.Deserialize<AiAdaptiveDecisionDto>(
            json,
            AiCoreJsonSerializerOptions.Instance);

        Assert.NotNull(roundTrip);
        Assert.Equal(action, roundTrip!.Action);
        Assert.Equal("بابا", roundTrip.OriginalTarget);
        Assert.Equal("با با", roundTrip.PracticeTarget);
        Assert.Equal(1, roundTrip.SimplificationLevel);
        Assert.Equal("wait_for_child", roundTrip.SessionPhase);
        Assert.Equal(1, roundTrip.HintsUsed);
        Assert.True(AdaptiveActions.IsKnown(action));
        Assert.DoesNotContain("Action", json); // camelCase only
        Assert.Contains("\"action\":", json);
    }

    [Fact]
    public void SessionExecution_Deserializes_From_FastApi_CamelCase()
    {
        string json = """
            {
              "attemptNumber": 1,
              "activityType": "word",
              "prompt": "بابا",
              "speechOutcome": "scored",
              "adaptiveDecision": {
                "action": "simplify",
                "reasonCodes": ["simplification_recommended"],
                "attemptNumber": 1,
                "scoreTrend": "stable",
                "requiresIntervention": false,
                "requiresDoctorReview": false,
                "recommendedDifficulty": "easier",
                "originalTarget": "بابا",
                "practiceTarget": "با با",
                "simplificationLevel": 1,
                "sessionPhase": "wait_for_child",
                "hintsUsed": 1
              },
              "avatar": { "spokenText": "حاول تاني" },
              "avatarGenerationMode": "deterministic_template",
              "sessionExecution": {
                "originalTarget": "بابا",
                "practiceTarget": "با با",
                "currentStep": 2,
                "attemptCount": 3,
                "hintsUsed": 1,
                "simplificationLevel": 1,
                "previousActions": ["retry_same", "retry_with_hint"],
                "successfulTargets": [],
                "difficultTargets": ["بابا"],
                "phase": "wait_for_child",
                "modeledCurrentTarget": false,
                "slowedDown": false
              }
            }
            """;

        AiEvaluateAttemptV2Response? response = JsonSerializer.Deserialize<AiEvaluateAttemptV2Response>(
            json,
            AiCoreJsonSerializerOptions.Instance);

        Assert.NotNull(response);
        Assert.Equal("simplify", response!.AdaptiveDecision.Action);
        Assert.Equal("بابا", response.AdaptiveDecision.OriginalTarget);
        Assert.Equal("با با", response.AdaptiveDecision.PracticeTarget);
        Assert.NotNull(response.SessionExecution);
        Assert.Equal("بابا", response.SessionExecution!.OriginalTarget);
        Assert.Equal("با با", response.SessionExecution.PracticeTarget);
        Assert.Equal(1, response.SessionExecution.HintsUsed);
        Assert.Equal(1, response.SessionExecution.SimplificationLevel);
        Assert.Equal("wait_for_child", response.SessionExecution.Phase);
        Assert.Equal(2, response.SessionExecution.PreviousActions.Count);
    }

    [Fact]
    public void Missing_SessionExecution_Remains_Null_For_Backward_Compatibility()
    {
        string json = """
            {
              "attemptNumber": 1,
              "activityType": "word",
              "prompt": "بابا",
              "speechOutcome": "scored",
              "adaptiveDecision": {
                "action": "advance",
                "reasonCodes": [],
                "attemptNumber": 1,
                "scoreTrend": "improving",
                "requiresIntervention": false,
                "requiresDoctorReview": false,
                "recommendedDifficulty": "same"
              },
              "avatar": { "spokenText": "ممتاز" },
              "avatarGenerationMode": "deterministic_template"
            }
            """;

        AiEvaluateAttemptV2Response? response = JsonSerializer.Deserialize<AiEvaluateAttemptV2Response>(
            json,
            AiCoreJsonSerializerOptions.Instance);

        Assert.NotNull(response);
        Assert.Null(response!.SessionExecution);
        Assert.Equal("advance", response.AdaptiveDecision.Action);
    }

    [Fact]
    public void Malformed_EvaluationJson_Does_Not_Throw()
    {
        Assert.Null(SessionExecutionSnapshot.TryReadFromEvaluationJson("{not-json"));
        Assert.Null(SessionExecutionSnapshot.TryReadFromEvaluationJson(null));
        Assert.Null(SessionExecutionSnapshot.TryReadFromEvaluationJson(""));
    }

    [Fact]
    public void Snapshot_Reads_SessionExecution_From_Stored_EvaluationJson()
    {
        var stored = new AiEvaluateAttemptV2Response
        {
            AttemptNumber = 2,
            ActivityType = "word",
            Prompt = "بابا",
            SpeechOutcome = "scored",
            AdaptiveDecision = new AiAdaptiveDecisionDto { Action = "retry_with_hint", HintsUsed = 1 },
            Avatar = new AiAvatarInstructionDto { SpokenText = "hint" },
            SessionExecution = new AiSessionExecutionDto
            {
                OriginalTarget = "بابا",
                PracticeTarget = "بابا",
                AttemptCount = 2,
                HintsUsed = 1,
                SimplificationLevel = 0,
                Phase = "wait_for_child"
            }
        };

        string json = JsonSerializer.Serialize(stored, AiCoreJsonSerializerOptions.Instance);
        AiSessionExecutionDto? restored = SessionExecutionSnapshot.TryReadFromEvaluationJson(json);

        Assert.NotNull(restored);
        Assert.Equal("بابا", restored!.OriginalTarget);
        Assert.Equal(1, restored.HintsUsed);
        Assert.Equal("wait_for_child", restored.Phase);
    }

    [Fact]
    public void SessionExecution_Dto_Has_Single_Json_Name_Per_Property()
    {
        var names = typeof(AiSessionExecutionDto)
            .GetProperties()
            .Select(p => p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), false)
                .Cast<System.Text.Json.Serialization.JsonPropertyNameAttribute>()
                .Single()
                .Name)
            .ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains("originalTarget", names);
        Assert.Contains("practiceTarget", names);
        Assert.DoesNotContain("targetValue", names);
    }

    [Fact]
    public void Unknown_Adaptive_Action_Is_Not_Treated_As_Known()
    {
        Assert.False(AdaptiveActions.IsKnown("some_future_action"));
        Assert.False(AdaptiveActions.IsKnown(null));
        Assert.True(AdaptiveActions.IsKnown("model"));
        Assert.True(AdaptiveActions.IsKnown("slow_down"));
    }
}
