using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Infrastructure.Ai;

namespace NomoAI.API.Tests;

public class AiCoreClientTests
{
    [Fact]
    public async Task Ready_Includes_Service_Key_Header()
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create();
        handler.Handler = (_, _) => Task.FromResult(JsonResponse("""
            {"status":"ready","service":"ai-core","version":"1.0.0","dependencies":[]}
            """));

        var result = await client.CheckReadinessAsync();

        Assert.True(result.IsSuccess);
        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal("/ready", request.RequestUri!.AbsolutePath);
        Assert.True(request.Headers.Contains(AiServiceOptions.ServiceKeyHeaderName));
        Assert.Equal(
            "test-service-key",
            request.Headers.GetValues(AiServiceOptions.ServiceKeyHeaderName).Single());
    }

    [Fact]
    public async Task Health_Does_Not_Require_Service_Key()
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create();
        handler.Handler = (_, _) => Task.FromResult(JsonResponse("""
            {"status":"ok","service":"ai-core","version":"1.0.0","environment":"local"}
            """));

        var result = await client.CheckHealthAsync();

        Assert.True(result.IsSuccess);
        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal("/health", request.RequestUri!.AbsolutePath);
        Assert.False(request.Headers.Contains(AiServiceOptions.ServiceKeyHeaderName));
    }

    [Fact]
    public async Task Correlation_Id_Is_Forwarded()
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create(correlationId: "corr-forward-xyz");
        handler.Handler = (_, _) => Task.FromResult(JsonResponse("""
            {"status":"ok","service":"ai-core","version":"1.0.0","environment":"local"}
            """));

        await client.CheckHealthAsync();

        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal(
            "corr-forward-xyz",
            request.Headers.GetValues(AiServiceOptions.CorrelationIdHeaderName).Single());
    }

    [Fact]
    public async Task PlanSession_Serializes_And_Deserializes()
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create();
        handler.Handler = async (request, _) =>
        {
            string body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"childId\":\"child-1\"", body);
            Assert.Contains("\"activityType\":\"word\"", body);
            Assert.Contains("\"targetValue\":\"بابا\"", body);
            Assert.Contains("\"speechLevel\":\"vocalization\"", body);

            return JsonResponse("""
                {
                  "sessionId":"s1",
                  "activityId":"a1",
                  "activityType":"word",
                  "targetValue":"بابا",
                  "title":"Plan",
                  "objective":"Practice",
                  "estimatedDurationMinutes":10,
                  "steps":[
                    {
                      "stepNumber":1,
                      "type":"introduction",
                      "instruction":"Say hi",
                      "expectedChildAction":"Listen",
                      "maximumAttempts":2,
                      "successCriteria":"Engage",
                      "avatar":{
                        "spokenText":"مرحبا",
                        "emotion":"friendly",
                        "gesture":"wave",
                        "speechRate":0.9,
                        "action":"introduce_target",
                        "nextStep":"demonstrate",
                        "segments":[]
                      }
                    }
                  ],
                  "completionCriteria":["Done"],
                  "safetyNotes":[],
                  "knowledgeSourceIds":["11111111-1111-1111-1111-111111111111"],
                  "knowledgeChunkIds":[],
                  "model":"test-model",
                  "usedFallback":false,
                  "regenerated":false,
                  "generatedAt":"2026-07-25T10:00:00Z"
                }
                """);
        };

        var result = await client.PlanSessionAsync(new AiSessionPlanRequest
        {
            ChildId = "child-1",
            ActivityId = "a1",
            ActivityType = "word",
            TargetValue = "بابا",
            SpeechLevel = "vocalization",
            Age = 8
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Plan", result.Value.Title);
        Assert.Equal("Practice", result.Value.Objective);
        Assert.Single(result.Value.Steps);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), result.Value.KnowledgeSourceIds[0]);
    }

    [Fact]
    public async Task EvaluateAttempt_Creates_Multipart_With_Filename_And_ContentType()
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create();
        handler.Handler = async (request, _) =>
        {
            Assert.Equal("/api/v1/sessions/attempts/evaluate", request.RequestUri!.AbsolutePath);
            MultipartFormDataContent multipart = Assert.IsType<MultipartFormDataContent>(request.Content);

            HttpContent audio = multipart.First(c =>
                c.Headers.ContentDisposition?.Name?.Trim('"') == "audio");

            Assert.Equal("sample.wav", audio.Headers.ContentDisposition!.FileName!.Trim('"'));
            Assert.Equal("audio/wav", audio.Headers.ContentType!.MediaType);

            Dictionary<string, string> fields = await ReadFormFieldsAsync(multipart);
            Assert.Equal("child-1", fields["childId"]);
            Assert.Equal("activity-1", fields["activityId"]);
            Assert.Equal("word", fields["activityType"]);
            Assert.Equal("بابا", fields["targetValue"]);
            Assert.Equal("1", fields["attemptNumber"]);

            return JsonResponse(EvaluateSuccessJson());
        };

        await using var audio = new MemoryStream(Encoding.UTF8.GetBytes("RIFF....WAVE"));
        var result = await client.EvaluateAttemptAsync(new AiEvaluateAttemptRequest
        {
            AudioStream = audio,
            FileName = "sample.wav",
            ContentType = "audio/wav",
            ChildId = "child-1",
            ActivityId = "activity-1",
            ActivityType = "word",
            TargetValue = "بابا",
            SpeechLevel = "vocalization",
            Age = 8,
            AttemptNumber = 1
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("scored", result.Value.SpeechOutcome);
        Assert.NotNull(result.Value.Avatar);
        Assert.Equal(2, result.Value.Avatar!.Segments.Count);
        Assert.Equal("future_emotion_value", result.Value.Avatar.Segments[1].Emotion);
        Assert.Equal("future_gesture_value", result.Value.Avatar.Segments[1].Gesture);
    }

    [Fact]
    public async Task Unknown_Avatar_Enum_Strings_Do_Not_Break_Deserialization()
    {
        string json = """
            {
              "spokenText":"hi",
              "emotion":"brand_new_emotion",
              "gesture":"brand_new_gesture",
              "speechRate":0.95,
              "action":"brand_new_action",
              "nextStep":"brand_new_next",
              "segments":[
                {
                  "text":"hi",
                  "emotion":"brand_new_emotion",
                  "gesture":"brand_new_gesture",
                  "speechRate":0.95,
                  "pauseAfterMs":200,
                  "expectedDurationMs":1000,
                  "frontendStateAfter":"brand_new_state"
                }
              ]
            }
            """;

        AiAvatarInstructionDto? avatar = JsonSerializer.Deserialize<AiAvatarInstructionDto>(
            json,
            AiCoreJsonSerializerOptions.Instance);

        Assert.NotNull(avatar);
        Assert.Equal("brand_new_emotion", avatar!.Emotion);
        Assert.Equal("brand_new_gesture", avatar.Gesture);
        Assert.Equal("brand_new_action", avatar.Action);
        Assert.Equal("brand_new_next", avatar.NextStep);
        Assert.Equal("brand_new_state", avatar.Segments[0].FrontendStateAfter);
    }

    [Fact]
    public async Task Status_422_Maps_To_InsufficientKnowledge()
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create();
        handler.Handler = (_, _) => Task.FromResult(JsonResponse("""
            {"error":{"code":"insufficient_approved_knowledge","message":"No knowledge","correlationId":"c1","details":{}}}
            """, HttpStatusCode.UnprocessableEntity));

        var result = await client.PlanSessionAsync(MinimalPlanRequest());

        Assert.True(result.IsFailure);
        Assert.Equal("AiService.InsufficientKnowledge", result.Error.Code);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.Error.StatusCode);
        // Response header correlation ID takes precedence over body correlationId.
        Assert.Equal("resp-corr", result.Error.CorrelationId);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "AiService.Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "AiService.Unauthorized")]
    public async Task Status_401_403_Map_To_Unauthorized(HttpStatusCode status, string expectedCode)
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create();
        handler.Handler = (_, _) => Task.FromResult(JsonResponse("""
            {"error":{"code":"unauthorized","message":"bad key","correlationId":"c-auth","details":{}}}
            """, status));

        var result = await client.CheckReadinessAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "AiService.RateLimited")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "AiService.Unavailable")]
    public async Task Status_429_And_503_Map_Correctly(HttpStatusCode status, string expectedCode)
    {
        var options = new AiServiceOptions
        {
            BaseUrl = "http://ai-core.test/",
            ServiceKey = "test-service-key",
            TimeoutSeconds = 30,
            HealthTimeoutSeconds = 5,
            MaxRetryAttempts = 0
        };

        var (client, handler, _) = AiCoreClientTestFactory.Create(options);
        handler.Handler = (_, _) => Task.FromResult(JsonResponse("""
            {"error":{"code":"busy","message":"try later","correlationId":"c-rate","details":{}}}
            """, status));

        var result = await client.PlanSessionAsync(MinimalPlanRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task Timeout_Is_Mapped_Correctly()
    {
        var options = new AiServiceOptions
        {
            BaseUrl = "http://ai-core.test/",
            ServiceKey = "test-service-key",
            TimeoutSeconds = 30,
            HealthTimeoutSeconds = 1,
            MaxRetryAttempts = 0
        };

        var (client, handler, _) = AiCoreClientTestFactory.Create(options);
        handler.Handler = async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
            return JsonResponse("""{"status":"ok"}""");
        };

        var result = await client.CheckHealthAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("AiService.Timeout", result.Error.Code);
    }

    [Fact]
    public async Task Invalid_Response_Json_Is_Handled_Safely()
    {
        var (client, handler, _) = AiCoreClientTestFactory.Create();
        handler.Handler = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{not-json", Encoding.UTF8, "application/json")
        });

        var result = await client.CheckHealthAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("AiService.InvalidResponse", result.Error.Code);
    }

    [Fact]
    public async Task Service_Key_And_Audio_Never_Appear_In_Logs()
    {
        var (client, handler, logger) = AiCoreClientTestFactory.Create();
        handler.Handler = (_, _) => Task.FromResult(JsonResponse(EvaluateSuccessJson()));

        await using var audio = new MemoryStream(Encoding.UTF8.GetBytes("SECRET_AUDIO_BYTES_SHOULD_NOT_LOG"));
        await client.EvaluateAttemptAsync(new AiEvaluateAttemptRequest
        {
            AudioStream = audio,
            FileName = "sample.wav",
            ContentType = "audio/wav",
            ChildId = "child-1",
            ActivityId = "activity-1",
            ActivityType = "word",
            TargetValue = "بابا",
            SpeechLevel = "vocalization",
            Age = 8,
            AttemptNumber = 1
        });

        string joined = string.Join('\n', logger.Messages);
        Assert.DoesNotContain("test-service-key", joined);
        Assert.DoesNotContain("SECRET_AUDIO_BYTES_SHOULD_NOT_LOG", joined);
        Assert.DoesNotContain("بابا", joined);
    }

    private static AiSessionPlanRequest MinimalPlanRequest() => new()
    {
        ChildId = "c",
        ActivityId = "a",
        ActivityType = "word",
        TargetValue = "x",
        SpeechLevel = "level",
        Age = 7
    };

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        response.Headers.TryAddWithoutValidation(AiServiceOptions.CorrelationIdHeaderName, "resp-corr");
        return response;
    }

    private static async Task<Dictionary<string, string>> ReadFormFieldsAsync(MultipartFormDataContent multipart)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (HttpContent part in multipart)
        {
            string? name = part.Headers.ContentDisposition?.Name?.Trim('"');
            if (string.IsNullOrWhiteSpace(name) || name == "audio")
            {
                continue;
            }

            fields[name] = await part.ReadAsStringAsync();
        }

        return fields;
    }

    private static string EvaluateSuccessJson() => """
        {
          "sessionId":"s1",
          "activityId":"activity-1",
          "attemptNumber":1,
          "activityType":"word",
          "targetValue":"بابا",
          "speechOutcome":"scored",
          "speechAnalysis":null,
          "adaptiveDecision":{
            "action":"retry_same",
            "reasonCodes":["first_attempt"],
            "attemptNumber":1,
            "scoreTrend":"not_available",
            "requiresIntervention":false,
            "requiresDoctorReview":false,
            "recommendedDifficulty":"same",
            "rulesVersion":"v1",
            "nextAllowedAttempt":2,
            "interventionCategories":[]
          },
          "knowledgeSourceIds":[],
          "avatar":{
            "spokenText":"شاطر! دورك.",
            "emotion":"encouraging",
            "gesture":"nod",
            "speechRate":0.9,
            "action":"ask_to_repeat",
            "nextStep":"repeat_current_target",
            "segments":[
              {
                "text":"شاطر!",
                "emotion":"encouraging",
                "gesture":"smile",
                "speechRate":0.9,
                "pauseAfterMs":200,
                "expectedDurationMs":800,
                "frontendStateAfter":null
              },
              {
                "text":"دورك.",
                "emotion":"future_emotion_value",
                "gesture":"future_gesture_value",
                "speechRate":0.9,
                "pauseAfterMs":100,
                "expectedDurationMs":700,
                "frontendStateAfter":"listening"
              }
            ]
          },
          "avatarModel":"test",
          "avatarGenerationMode":"llm",
          "usedFallback":false,
          "generatedAt":"2026-07-25T10:00:00Z"
        }
        """;
}
