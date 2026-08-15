using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NomoAI.API.Common.Ai;
using NomoAI.API.Features.Sessions.Ai.EvaluateAttempt;
using NomoAI.API.Infrastructure.Ai;

namespace NomoAI.API.Tests;

public class EvaluateAttemptOpenApiTests
{
    [Fact]
    public void OperationFilter_Rewrites_Request_As_Multipart_With_Binary_Audio()
    {
        var operation = new OpenApiOperation
        {
            OperationId = EvaluateAttemptFormOperationFilter.OperationId
        };

        var filter = new EvaluateAttemptFormOperationFilter();
        filter.Apply(operation, context: null!);

        Assert.NotNull(operation.RequestBody);
        Assert.True(operation.RequestBody.Required);
        Assert.True(operation.RequestBody.Content.ContainsKey("multipart/form-data"));

        OpenApiMediaType media = operation.RequestBody.Content["multipart/form-data"];
        Assert.Equal("object", media.Schema.Type);
        Assert.Contains("Audio", media.Schema.Required);
        Assert.Equal("string", media.Schema.Properties["Audio"].Type);
        Assert.Equal("binary", media.Schema.Properties["Audio"].Format);

        string[] expectedFields =
        [
            "Audio",
            "TargetValue",
            "ConsecutiveNoSpeechCount",
            "SpeechLevel",
            "MaximumAttempts",
            "AdditionalContext",
            "ActivityType",
            "ActivityId",
            "SessionStepNumber",
            "SessionId",
            "PreviousDecision",
            "AttemptNumber",
            "ChildId",
            "PreviousAttemptScores",
            "Language",
            "Age"
        ];

        foreach (string field in expectedFields)
        {
            Assert.True(
                media.Schema.Properties.ContainsKey(field),
                $"Missing OpenAPI form field: {field}");
        }

        Assert.True(operation.Parameters is null || operation.Parameters.Count == 0);
    }

    [Fact]
    public void BuildSchema_Marks_Audio_As_Required_Binary()
    {
        OpenApiSchema schema = EvaluateAttemptFormOperationFilter.BuildSchema();

        Assert.Contains(nameof(EvaluateAttemptFormRequest.Audio), schema.Required);
        Assert.Equal("binary", schema.Properties[nameof(EvaluateAttemptFormRequest.Audio)].Format);
    }
}

public class EvaluateAttemptCommandValidatorTests
{
    private readonly EvaluateAttemptCommandValidator _validator = new(
        Options.Create(new AiServiceOptions
        {
            BaseUrl = "http://localhost:8000",
            ServiceKey = "test",
            MaxAudioBytes = 1024
        }));

    [Fact]
    public void Rejects_Missing_Audio_Stream()
    {
        var command = ValidCommand() with
        {
            AudioStream = null,
            AudioLengthBytes = 0,
            FileName = string.Empty,
            ContentType = string.Empty
        };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateAttemptCommand.AudioStream));
    }

    [Fact]
    public void Rejects_Oversized_Audio()
    {
        using var stream = new MemoryStream(new byte[2048]);
        var command = ValidCommand() with
        {
            AudioStream = stream,
            AudioLengthBytes = 2048
        };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateAttemptCommand.AudioLengthBytes));
    }

    [Fact]
    public void Rejects_Unsupported_Content_Type()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = ValidCommand() with
        {
            AudioStream = stream,
            AudioLengthBytes = 3,
            ContentType = "text/plain"
        };

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(EvaluateAttemptCommand.ContentType));
    }

    [Fact]
    public void Accepts_Valid_Audio_Metadata()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = ValidCommand() with
        {
            AudioStream = stream,
            AudioLengthBytes = 3
        };

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    private static EvaluateAttemptCommand ValidCommand() => new(
        AudioStream: Stream.Null,
        FileName: "sample.wav",
        ContentType: "audio/wav",
        AudioLengthBytes: 3,
        ChildId: "child-1",
        ActivityId: "activity-1",
        ActivityType: "word",
        TargetValue: "بابا",
        SpeechLevel: "vocalization",
        Age: 8,
        AttemptNumber: 1,
        SessionId: null,
        SessionStepNumber: null,
        MaximumAttempts: null,
        PreviousAttemptScoresJson: null,
        PreviousDecision: null,
        ConsecutiveNoSpeechCount: 0,
        Language: "ar",
        AdditionalContext: null);
}

public class EvaluateAttemptFormRequestContractTests
{
    [Fact]
    public void Form_Request_Exposes_Required_Public_Properties()
    {
        Type type = typeof(EvaluateAttemptFormRequest);
        string[] names =
        [
            nameof(EvaluateAttemptFormRequest.Audio),
            nameof(EvaluateAttemptFormRequest.TargetValue),
            nameof(EvaluateAttemptFormRequest.ConsecutiveNoSpeechCount),
            nameof(EvaluateAttemptFormRequest.SpeechLevel),
            nameof(EvaluateAttemptFormRequest.MaximumAttempts),
            nameof(EvaluateAttemptFormRequest.AdditionalContext),
            nameof(EvaluateAttemptFormRequest.ActivityType),
            nameof(EvaluateAttemptFormRequest.ActivityId),
            nameof(EvaluateAttemptFormRequest.SessionStepNumber),
            nameof(EvaluateAttemptFormRequest.SessionId),
            nameof(EvaluateAttemptFormRequest.PreviousDecision),
            nameof(EvaluateAttemptFormRequest.AttemptNumber),
            nameof(EvaluateAttemptFormRequest.ChildId),
            nameof(EvaluateAttemptFormRequest.PreviousAttemptScores),
            nameof(EvaluateAttemptFormRequest.Language),
            nameof(EvaluateAttemptFormRequest.Age),
            nameof(EvaluateAttemptFormRequest.SessionExecution)
        ];

        foreach (string name in names)
        {
            Assert.NotNull(type.GetProperty(name));
        }

        Assert.Equal(typeof(IFormFile), type.GetProperty(nameof(EvaluateAttemptFormRequest.Audio))!.PropertyType);
    }
}
