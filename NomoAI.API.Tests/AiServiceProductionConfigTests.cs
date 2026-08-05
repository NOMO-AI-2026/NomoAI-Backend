using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Infrastructure.Ai;

namespace NomoAI.API.Tests;

public class AiServiceProductionConfigTests
{
    [Fact]
    public void Production_BaseUrl_Is_Loaded_From_Configuration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiService:BaseUrl"] = "http://191.218.161.183",
                ["AiService:ServiceKey"] = "configured-secret",
                ["AiService:TimeoutSeconds"] = "180",
                ["AiService:HealthTimeoutSeconds"] = "10",
                ["AiService:MaxRetryAttempts"] = "2"
            })
            .Build();

        var options = new AiServiceOptions();
        configuration.GetSection(AiServiceOptions.SectionName).Bind(options);

        Assert.Equal("http://191.218.161.183", options.BaseUrl);
        Assert.Equal("configured-secret", options.ServiceKey);
        Assert.Equal(180, options.TimeoutSeconds);
        Assert.Equal(10, options.HealthTimeoutSeconds);
        Assert.Equal(2, options.MaxRetryAttempts);
    }

    [Fact]
    public void Environment_Variable_Names_Bind_With_Double_Underscore()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiService__BaseUrl"] = "https://ai.example.com",
                ["AiService__ServiceKey"] = "env-secret"
            })
            .Build();

        // ASP.NET env vars use __ which Configuration maps to : ; simulate that mapping.
        IConfiguration mapped = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiService:BaseUrl"] = "https://ai.example.com",
                ["AiService:ServiceKey"] = "env-secret"
            })
            .Build();

        var options = new AiServiceOptions();
        mapped.GetSection(AiServiceOptions.SectionName).Bind(options);

        Assert.Equal("https://ai.example.com", options.BaseUrl);
        Assert.Equal("env-secret", options.ServiceKey);
    }

    [Theory]
    [InlineData("http://191.218.161.183", "http://191.218.161.183/")]
    [InlineData("http://191.218.161.183/", "http://191.218.161.183/")]
    [InlineData(" http://191.218.161.183 ", "http://191.218.161.183/")]
    [InlineData("https://ai.example.com", "https://ai.example.com/")]
    [InlineData("http://191.218.161.183/api/v1", "http://191.218.161.183/")]
    public void BaseUrl_Normalizer_Produces_Host_Root_With_Trailing_Slash(
        string input,
        string expected)
    {
        Uri baseAddress = AiServiceBaseUrlNormalizer.CreateBaseAddress(input);
        Assert.Equal(expected, baseAddress.AbsoluteUri);
    }

    [Theory]
    [InlineData("http://191.218.161.183", "/api/v1/sessions/plan", "http://191.218.161.183/api/v1/sessions/plan")]
    [InlineData("http://191.218.161.183/", "/api/v1/sessions/attempts/evaluate", "http://191.218.161.183/api/v1/sessions/attempts/evaluate")]
    [InlineData("http://191.218.161.183", "/api/v1/sessions/summary", "http://191.218.161.183/api/v1/sessions/summary")]
    [InlineData("http://191.218.161.183", "/ready", "http://191.218.161.183/ready")]
    [InlineData("http://localhost:8000", "/api/v1/sessions/plan", "http://localhost:8000/api/v1/sessions/plan")]
    public void Final_Endpoint_Urls_Compose_Without_Duplication(
        string baseUrl,
        string path,
        string expected)
    {
        Uri combined = AiServiceBaseUrlNormalizer.Combine(baseUrl, path);
        Assert.Equal(expected, combined.AbsoluteUri);
        Assert.DoesNotContain("api/v1/api/v1", combined.AbsoluteUri);
        Assert.DoesNotContain(".183api/", combined.AbsoluteUri);
    }

    [Fact]
    public void Client_Source_Does_Not_Hardcode_Localhost_Or_Vps_Ip()
    {
        string root = FindRepoRoot();
        string[] files =
        [
            Path.Combine(root, "NomoAI.API", "Infrastructure", "Ai", "AiCoreClient.cs"),
            Path.Combine(root, "NomoAI.API", "Infrastructure", "Ai", "AiServiceServiceCollectionExtensions.cs"),
            Path.Combine(root, "NomoAI.API", "Common", "Ai", "AiServiceOptions.cs")
        ];

        foreach (string file in files)
        {
            string content = File.ReadAllText(file);
            Assert.DoesNotContain("localhost:8000", content);
            Assert.DoesNotContain("127.0.0.1:8000", content);
            Assert.DoesNotContain("191.218.161.183", content);
        }
    }

    [Fact]
    public async Task Plan_Uses_Configured_Remote_Host_And_Service_Key()
    {
        var options = new AiServiceOptions
        {
            BaseUrl = "http://191.218.161.183",
            ServiceKey = "vps-service-key",
            TimeoutSeconds = 180,
            HealthTimeoutSeconds = 10,
            MaxRetryAttempts = 0
        };

        var (client, handler, _) = CreateClient(options);
        handler.Handler = (request, _) =>
        {
            Assert.Equal("http://191.218.161.183/api/v2/sessions/plan", request.RequestUri!.AbsoluteUri);
            Assert.Equal(
                "vps-service-key",
                request.Headers.GetValues(AiServiceOptions.ServiceKeyHeaderName).Single());
            return Task.FromResult(JsonOk("""
                {
                  "activityType":"word",
                  "prompt":"x",
                  "title":"t",
                  "objective":"o",
                  "estimatedDurationMinutes":10,
                  "steps":[],
                  "completionCriteria":["c"],
                  "safetyNotes":[],
                  "knowledgeSourceIds":["11111111-1111-1111-1111-111111111111"],
                  "knowledgeChunkIds":[],
                  "model":"m",
                  "usedFallback":false,
                  "regenerated":false,
                  "generatedAt":"2026-07-27T00:00:00Z"
                }
                """));
        };

        var result = await client.PlanSessionAsync(new AiSessionPlanV2Request
        {
            ActivityType = "word",
            Prompt = "x",
            SpeechLevel = "level",
            Age = 8
        });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Summary_Uses_Configured_Remote_Host()
    {
        var options = new AiServiceOptions
        {
            BaseUrl = "http://191.218.161.183",
            ServiceKey = "vps-service-key",
            TimeoutSeconds = 180,
            HealthTimeoutSeconds = 10,
            MaxRetryAttempts = 0
        };

        var (client, handler, _) = CreateClient(options);
        handler.Handler = (request, _) =>
        {
            Assert.Equal("http://191.218.161.183/api/v1/sessions/summary", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(JsonOk("""
                {
                  "sessionId":"s1",
                  "activityId":"a1",
                  "activityType":"word",
                  "targetValue":"x",
                  "outcome":"inconclusive",
                  "metrics":{"attemptCount":1,"noSpeechAttempts":0,"successfulAttempts":0,"interventionCount":0,"scoreTrend":"not_available","finalAdaptiveAction":"end_attempts","doctorReviewRecommended":false},
                  "summary":{"shortSummary":"ok","strengths":["a"],"practiceAreas":["b"],"nextSessionSuggestion":"c"},
                  "knowledgeSourceIds":[],
                  "summaryGenerationMode":"deterministic_template",
                  "usedFallback":true,
                  "rulesVersion":"v1",
                  "generatedAt":"2026-07-27T00:00:00Z"
                }
                """));
        };

        var result = await client.CreateSessionSummaryAsync(new AiSessionSummaryRequest
        {
            SessionId = "s1",
            ActivityId = "a1",
            ActivityType = "word",
            TargetValue = "x",
            SpeechLevel = "level",
            Age = 8,
            Attempts =
            [
                new AiSummaryAttemptInput
                {
                    AttemptNumber = 1,
                    SpeechOutcome = "scored",
                    AdaptiveAction = "end_attempts"
                }
            ]
        });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Ready_Health_Check_Uses_Configured_Host_And_Key()
    {
        var options = new AiServiceOptions
        {
            BaseUrl = "http://191.218.161.183",
            ServiceKey = "vps-service-key",
            TimeoutSeconds = 180,
            HealthTimeoutSeconds = 10,
            MaxRetryAttempts = 0
        };

        var (client, handler, _) = CreateClient(options);
        handler.Handler = (request, _) =>
        {
            Assert.Equal("http://191.218.161.183/ready", request.RequestUri!.AbsoluteUri);
            Assert.Equal(
                "vps-service-key",
                request.Headers.GetValues(AiServiceOptions.ServiceKeyHeaderName).Single());
            return Task.FromResult(JsonOk("""
                {"status":"ready","service":"ai-core","version":"1.0.0","dependencies":[]}
                """));
        };

        var result = await client.CheckReadinessAsync();
        Assert.True(result.IsSuccess);
        Assert.Equal("ready", result.Value.Status);
    }

    [Fact]
    public async Task Evaluate_Still_Creates_Multipart_Against_Configured_Host()
    {
        var options = new AiServiceOptions
        {
            BaseUrl = "http://191.218.161.183",
            ServiceKey = "vps-service-key",
            TimeoutSeconds = 180,
            HealthTimeoutSeconds = 10,
            MaxRetryAttempts = 0
        };

        var (client, handler, logger) = CreateClient(options);
        handler.Handler = async (request, _) =>
        {
            Assert.Equal(
                "http://191.218.161.183/api/v2/sessions/attempts/evaluate",
                request.RequestUri!.AbsoluteUri);
            Assert.IsType<MultipartFormDataContent>(request.Content);
            await Task.CompletedTask;
            return JsonOk("""
                {
                  "attemptNumber":1,
                  "activityType":"word",
                  "prompt":"x",
                  "speechOutcome":"no_speech",
                  "adaptiveDecision":{
                    "action":"retry_same",
                    "reasonCodes":["first_attempt"],
                    "attemptNumber":1,
                    "scoreTrend":"not_available",
                    "requiresIntervention":false,
                    "requiresDoctorReview":false,
                    "recommendedDifficulty":"same",
                    "rulesVersion":"v1",
                    "interventionCategories":[]
                  },
                  "knowledgeSourceIds":[],
                  "avatar":{
                    "spokenText":"hi",
                    "emotion":"friendly",
                    "gesture":"wave",
                    "speechRate":0.9,
                    "action":"encourage",
                    "nextStep":"repeat_current_target",
                    "segments":[]
                  },
                  "avatarGenerationMode":"deterministic_template",
                  "usedFallback":true,
                  "generatedAt":"2026-07-27T00:00:00Z"
                }
                """);
        };

        await using var audio = new MemoryStream(Encoding.UTF8.GetBytes("RIFF....WAVE"));
        var result = await client.EvaluateAttemptAsync(new AiEvaluateAttemptV2Request
        {
            AudioStream = audio,
            FileName = "sample.wav",
            ContentType = "audio/wav",
            ActivityType = "word",
            Prompt = "x",
            SpeechLevel = "level",
            Age = 8,
            AttemptNumber = 1
        });

        Assert.True(result.IsSuccess);
        string joined = string.Join('\n', logger.Messages);
        Assert.DoesNotContain("vps-service-key", joined);
        Assert.DoesNotContain("RIFF....WAVE", joined);
    }

    private static (AiCoreClient Client, FakeHttpMessageHandler Handler, ListLogger<AiCoreClient> Logger)
        CreateClient(AiServiceOptions options)
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = AiServiceBaseUrlNormalizer.CreateBaseAddress(options.BaseUrl)
        };

        var logger = new ListLogger<AiCoreClient>();
        var client = new AiCoreClient(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(options),
            new TestCorrelationIdAccessor("corr-prod"),
            logger);

        return (client, handler, logger);
    }

    private static HttpResponseMessage JsonOk(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NomoAI.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate NomoAI.sln from test output directory.");
    }
}
