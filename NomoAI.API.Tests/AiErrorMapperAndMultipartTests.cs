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
        using var content = AiCoreClient.BuildMultipartContent(new AiEvaluateAttemptRequest
        {
            AudioStream = stream,
            FileName = "a.wav",
            ContentType = "audio/wav",
            ChildId = "c1",
            ActivityId = "a1",
            ActivityType = "word",
            TargetValue = "t",
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
}
