using System.Net;
using System.Text;
using System.Text.Json;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime;
using NomoAI.API.Features.Sessions.Runtime.SubmitAttempt;

namespace NomoAI.API.Tests;

public class SessionPlanSnapshotTests
{
    [Fact]
    public void Serialize_Then_Deserialize_Roundtrips_Plan()
    {
        AiSessionPlanV2Response plan = MakePlan(stepCount: 2);

        string json = SessionPlanSnapshot.Serialize(plan);
        AiSessionPlanV2Response? roundTripped = SessionPlanSnapshot.Deserialize(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(plan.Title, roundTripped!.Title);
        Assert.Equal(2, roundTripped.Steps.Count);
    }

    [Fact]
    public void Deserialize_Returns_Null_For_Empty_Or_Invalid_Json()
    {
        Assert.Null(SessionPlanSnapshot.Deserialize(null));
        Assert.Null(SessionPlanSnapshot.Deserialize(""));
        Assert.Null(SessionPlanSnapshot.Deserialize("{not-json"));
    }

    [Fact]
    public void GetStep_Finds_Step_By_Number_Or_Returns_Null()
    {
        AiSessionPlanV2Response plan = MakePlan(stepCount: 3);

        Assert.NotNull(SessionPlanSnapshot.GetStep(plan, 2));
        Assert.Equal(2, SessionPlanSnapshot.GetStep(plan, 2)!.StepNumber);
        Assert.Null(SessionPlanSnapshot.GetStep(plan, 99));
    }

    internal static AiSessionPlanV2Response MakePlan(int stepCount, int maximumAttempts = 3) => new()
    {
        ActivityType = "word",
        Prompt = "بابا",
        Title = "Practice Session",
        Objective = "Say the word",
        EstimatedDurationMinutes = 10,
        Steps = Enumerable.Range(1, stepCount)
            .Select(number => new AiSessionStepDto
            {
                StepNumber = number,
                Type = number == 1 ? "introduction" : "guided_practice",
                Instruction = $"Step {number}",
                ExpectedChildAction = "Repeat the word",
                MaximumAttempts = maximumAttempts,
                Avatar = new AiAvatarInstructionDto
                {
                    SpokenText = $"Spoken text for step {number}",
                    Emotion = "encouraging"
                }
            })
            .ToArray()
    };
}

public class SessionStepTypesTests
{
    [Theory]
    [InlineData("guided_practice", "Repeat the word", true)]
    [InlineData("independent_attempt", "Say it alone", true)]
    [InlineData("introduction", "Listen carefully", false)]
    [InlineData("demonstration", "Watch the avatar", false)]
    [InlineData("completion", "Celebrate", false)]
    [InlineData("review", "Look back", false)]
    public void ExpectsChildResponse_Matches_Known_Step_Types(string stepType, string action, bool expected)
    {
        Assert.Equal(expected, SessionStepTypes.ExpectsChildResponse(stepType, action));
    }

    [Theory]
    [InlineData("reinforcement", "كرر الكلمة معي", true)]
    [InlineData("reinforcement", "just watch and relax", false)]
    public void ExpectsChildResponse_Infers_From_Instruction_For_Unknown_Types(
        string stepType, string action, bool expected)
    {
        Assert.Equal(expected, SessionStepTypes.ExpectsChildResponse(stepType, action));
    }
}

public class AdaptiveTransitionTests
{
    [Fact]
    public void Advance_Moves_To_Next_Step_When_Available()
    {
        Session session = MakeInProgressSession(currentStep: 1, currentAttempt: 2);
        AiSessionPlanV2Response plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2);

        SubmitAttemptCommandHandler.ApplyAdaptiveTransition(session, plan, "advance");

        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.Equal(2, session.CurrentStepNumber);
        Assert.Equal(0, session.CurrentAttemptNumber);
    }

    [Fact]
    public void Advance_Completes_Session_When_No_Next_Step()
    {
        Session session = MakeInProgressSession(currentStep: 2, currentAttempt: 1);
        AiSessionPlanV2Response plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2);

        SubmitAttemptCommandHandler.ApplyAdaptiveTransition(session, plan, "advance");

        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.NotNull(session.EndedAt);
    }

    [Theory]
    [InlineData("retry_same")]
    [InlineData("retry_with_hint")]
    [InlineData("simplify")]
    [InlineData("ask_follow_up")]
    [InlineData("continue_conversation")]
    [InlineData("take_short_break")]
    public void Retry_Style_Actions_Increment_Attempt_And_Stay_In_Progress(string action)
    {
        Session session = MakeInProgressSession(currentStep: 1, currentAttempt: 0);
        AiSessionPlanV2Response plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2);

        SubmitAttemptCommandHandler.ApplyAdaptiveTransition(session, plan, action);

        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.Equal(1, session.CurrentStepNumber);
        Assert.Equal(1, session.CurrentAttemptNumber);
    }

    [Fact]
    public void Retry_When_Attempt_Budget_Exhausted_Advances_To_Next_Step()
    {
        Session session = MakeInProgressSession(currentStep: 1, currentAttempt: 0);
        AiSessionPlanV2Response plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2, maximumAttempts: 1);

        SubmitAttemptCommandHandler.ApplyAdaptiveTransition(session, plan, "retry_same");

        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.Equal(2, session.CurrentStepNumber);
        Assert.Equal(0, session.CurrentAttemptNumber);
    }

    [Fact]
    public void Retry_When_Attempt_Budget_Exhausted_On_Last_Step_Completes_Session()
    {
        Session session = MakeInProgressSession(currentStep: 2, currentAttempt: 0);
        AiSessionPlanV2Response plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2, maximumAttempts: 1);

        SubmitAttemptCommandHandler.ApplyAdaptiveTransition(session, plan, "retry_same");

        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.NotNull(session.EndedAt);
    }

    [Theory]
    [InlineData("end")]
    [InlineData("end_attempts")]
    public void End_Actions_Complete_The_Session(string action)
    {
        Session session = MakeInProgressSession(currentStep: 1, currentAttempt: 1);
        AiSessionPlanV2Response plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2);

        SubmitAttemptCommandHandler.ApplyAdaptiveTransition(session, plan, action);

        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.NotNull(session.EndedAt);
        Assert.False(session.RequiresDoctorReview);
    }

    [Fact]
    public void RecommendDoctorReview_Flags_And_Completes_Session()
    {
        Session session = MakeInProgressSession(currentStep: 1, currentAttempt: 2);
        AiSessionPlanV2Response plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2);

        SubmitAttemptCommandHandler.ApplyAdaptiveTransition(session, plan, "recommend_doctor_review");

        Assert.True(session.RequiresDoctorReview);
        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.NotNull(session.EndedAt);
    }

    [Fact]
    public void Unknown_Action_Falls_Back_To_Retry_Behavior()
    {
        Session session = MakeInProgressSession(currentStep: 1, currentAttempt: 0);
        AiSessionPlanV2Response plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2);

        SubmitAttemptCommandHandler.ApplyAdaptiveTransition(session, plan, "some_future_action");

        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.Equal(1, session.CurrentAttemptNumber);
    }

    private static Session MakeInProgressSession(int currentStep, int currentAttempt) => new()
    {
        Id = 1,
        ChildId = 1,
        ActivityId = 1,
        SessionTitle = "Test session",
        Status = SessionStatus.InProgress,
        CurrentStepNumber = currentStep,
        CurrentAttemptNumber = currentAttempt,
        ActivityType = "word",
        Prompt = "بابا",
        Language = "ar"
    };
}

public class SynthesizeSpeechAsyncTests
{
    [Fact]
    public async Task Synthesizes_Then_Downloads_Audio_Stream()
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create();
        var audioBytes = Encoding.UTF8.GetBytes("FAKE_MP3_BYTES");
        string? capturedSynthesizeBody = null;

        handler.Handler = async (request, _) =>
        {
            if (request.Method == HttpMethod.Post &&
                request.RequestUri!.AbsolutePath == "/api/v1/speech/synthesize")
            {
                capturedSynthesizeBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {"audioId":"audio-123","audioUrl":"","contentType":"audio/mpeg","format":"mp3","sizeBytes":14,"cached":false,"model":"tts-1","voice":"default"}
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
            }

            if (request.Method == HttpMethod.Get &&
                request.RequestUri!.AbsolutePath == "/api/v1/speech/audio/audio-123")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(audioBytes)
                };
                response.Content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
                return response;
            }

            throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");
        };

        var result = await client.SynthesizeSpeechAsync("مرحبا", purpose: "instruction");

        Assert.True(result.IsSuccess);
        Assert.Equal("audio/mpeg", result.Value.ContentType);
        Assert.Equal("audio-123", result.Value.AudioId);

        using var reader = new StreamReader(result.Value.Content);
        string downloaded = await reader.ReadToEndAsync();
        Assert.Equal("FAKE_MP3_BYTES", downloaded);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Contains("\"format\":\"wav\"", capturedSynthesizeBody);

        result.Value.Dispose();
    }

    [Fact]
    public async Task Empty_Text_Fails_Without_Calling_Ai_Core()
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create();

        var result = await client.SynthesizeSpeechAsync("   ");

        Assert.True(result.IsFailure);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Download_Failure_After_Successful_Synthesize_Is_Surfaced()
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create();

        handler.Handler = (request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/speech/synthesize")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {"audioId":"audio-404","audioUrl":"","contentType":"audio/mpeg","format":"mp3","sizeBytes":0,"cached":false,"model":"tts-1","voice":"default"}
                        """,
                        Encoding.UTF8,
                        "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    """{"error":{"code":"not_found","message":"missing","correlationId":"c1","details":{}}}""",
                    Encoding.UTF8,
                    "application/json")
            });
        };

        var result = await client.SynthesizeSpeechAsync("hello");

        Assert.True(result.IsFailure);
    }
}
