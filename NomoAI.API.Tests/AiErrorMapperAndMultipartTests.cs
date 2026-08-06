using System.Text.Json;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Infrastructure.Ai;

namespace NomoAI.API.Tests;

public class AiErrorMapperTests
{
    [Fact]
    public void MapHttpStatus_Uses_Remote_CorrelationId()
    {
        var error = AiServiceErrorMapper.MapHttpStatus(
            System.Net.HttpStatusCode.BadRequest,
            """{"error":{"code":"validation_error","message":"bad","correlationId":"remote-corr","details":{}}}""",
            responseCorrelationId: null,
            fallbackCorrelationId: "local-corr");

        Assert.Equal("AiService.InvalidRequest", error.Code);
        Assert.Equal("remote-corr", error.CorrelationId);
    }

    [Fact]
    public void MapHttpStatus_Maps_PaymentRequired_To_InsufficientCredit()
    {
        var error = AiServiceErrorMapper.MapHttpStatus(
            System.Net.HttpStatusCode.PaymentRequired,
            """{"error":{"code":"tts_insufficient_credit","message":"credits","correlationId":"c-402","details":{}}}""",
            responseCorrelationId: null,
            fallbackCorrelationId: "local-corr");

        Assert.Equal("AiService.InsufficientCredit", error.Code);
        Assert.Equal(402, error.StatusCode);
        Assert.Equal("c-402", error.CorrelationId);
    }

    [Fact]
    public void Invalid_Json_Body_Still_Maps_Status()
    {
        var error = AiServiceErrorMapper.MapHttpStatus(
            System.Net.HttpStatusCode.ServiceUnavailable,
            "not-json",
            responseCorrelationId: "hdr-corr",
            fallbackCorrelationId: "local-corr");

        Assert.Equal("AiService.Unavailable", error.Code);
        Assert.Equal("hdr-corr", error.CorrelationId);
    }
}

public class MultipartContentTests
{
    [Fact]
    public async Task BuildMultipartContent_Includes_Previous_Scores_Json()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        using var content = AiCoreClient.BuildMultipartContent(new AiEvaluateAttemptV2Request
        {
            AudioStream = stream,
            FileName = "a.wav",
            ContentType = "audio/wav",
            ActivityType = "word",
            Prompt = "t",
            SpeechLevel = "level",
            Age = 6,
            AttemptNumber = 2,
            PreviousAttemptScores = [40.5, 55]
        });

        HttpContent scoresPart = content.First(c =>
            c.Headers.ContentDisposition?.Name?.Trim('"') == "previousAttemptScores");

        string scores = await scoresPart.ReadAsStringAsync();
        double[]? parsed = JsonSerializer.Deserialize<double[]>(scores);
        Assert.Equal([40.5, 55], parsed);
    }

    [Fact]
    public async Task BuildMultipartContent_Does_Not_Include_Database_Ids()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        using var content = AiCoreClient.BuildMultipartContent(new AiEvaluateAttemptV2Request
        {
            AudioStream = stream,
            FileName = "a.wav",
            ContentType = "audio/wav",
            ActivityType = "word",
            Prompt = "بابا",
            SpeechLevel = "level",
            Age = 6,
            AttemptNumber = 1
        });

        var fieldNames = content
            .Select(c => c.Headers.ContentDisposition?.Name?.Trim('"'))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("childId", fieldNames);
        Assert.DoesNotContain("activityId", fieldNames);
        Assert.DoesNotContain("sessionId", fieldNames);
        Assert.DoesNotContain("targetValue", fieldNames);
        Assert.Contains("prompt", fieldNames);
        Assert.Contains("activityType", fieldNames);
    }
}
