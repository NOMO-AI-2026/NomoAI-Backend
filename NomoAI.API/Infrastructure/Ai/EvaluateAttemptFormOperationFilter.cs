using Microsoft.OpenApi.Models;
using NomoAI.API.Features.Sessions.Ai.EvaluateAttempt;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NomoAI.API.Infrastructure.Ai;

/// <summary>
/// Swashbuckle treats [FromForm] complex types on Minimal APIs as a single JSON-like
/// "form" body. This filter rewrites EvaluateAttemptAi as true multipart/form-data
/// with a binary Audio field and individual form properties.
/// </summary>
public sealed class EvaluateAttemptFormOperationFilter : IOperationFilter
{
    public const string OperationId = "EvaluateAttemptAi";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(operation.OperationId, OperationId, StringComparison.Ordinal))
        {
            return;
        }

        // Remove any incorrectly generated form/body parameters.
        operation.Parameters?.Clear();

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Description =
                "Multipart attempt evaluation. Audio is required; other fields are attempt metadata.",
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = BuildSchema(),
                    Encoding = new Dictionary<string, OpenApiEncoding>
                    {
                        ["Audio"] = new OpenApiEncoding
                        {
                            Style = ParameterStyle.Form
                        }
                    }
                }
            }
        };
    }

    public static OpenApiSchema BuildSchema()
    {
        return new OpenApiSchema
        {
            Type = "object",
            Required = new HashSet<string>
            {
                nameof(EvaluateAttemptFormRequest.Audio),
                nameof(EvaluateAttemptFormRequest.ChildId),
                nameof(EvaluateAttemptFormRequest.ActivityId),
                nameof(EvaluateAttemptFormRequest.ActivityType),
                nameof(EvaluateAttemptFormRequest.TargetValue),
                nameof(EvaluateAttemptFormRequest.SpeechLevel),
                nameof(EvaluateAttemptFormRequest.Age),
                nameof(EvaluateAttemptFormRequest.AttemptNumber)
            },
            Properties = new Dictionary<string, OpenApiSchema>
            {
                [nameof(EvaluateAttemptFormRequest.Audio)] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = "Audio attempt file (wav, mp3, m4a, webm, or ogg)."
                },
                [nameof(EvaluateAttemptFormRequest.TargetValue)] = new OpenApiSchema
                {
                    Type = "string",
                    Description = "Therapy target value (no PII)."
                },
                [nameof(EvaluateAttemptFormRequest.ConsecutiveNoSpeechCount)] = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32"
                },
                [nameof(EvaluateAttemptFormRequest.SpeechLevel)] = new OpenApiSchema
                {
                    Type = "string"
                },
                [nameof(EvaluateAttemptFormRequest.MaximumAttempts)] = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32",
                    Nullable = true
                },
                [nameof(EvaluateAttemptFormRequest.AdditionalContext)] = new OpenApiSchema
                {
                    Type = "string",
                    Nullable = true
                },
                [nameof(EvaluateAttemptFormRequest.ActivityType)] = new OpenApiSchema
                {
                    Type = "string",
                    Description = "character | word | sentence"
                },
                [nameof(EvaluateAttemptFormRequest.ActivityId)] = new OpenApiSchema
                {
                    Type = "string"
                },
                [nameof(EvaluateAttemptFormRequest.SessionStepNumber)] = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32",
                    Nullable = true
                },
                [nameof(EvaluateAttemptFormRequest.SessionId)] = new OpenApiSchema
                {
                    Type = "string",
                    Nullable = true
                },
                [nameof(EvaluateAttemptFormRequest.PreviousDecision)] = new OpenApiSchema
                {
                    Type = "string",
                    Nullable = true
                },
                [nameof(EvaluateAttemptFormRequest.AttemptNumber)] = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32"
                },
                [nameof(EvaluateAttemptFormRequest.ChildId)] = new OpenApiSchema
                {
                    Type = "string"
                },
                [nameof(EvaluateAttemptFormRequest.PreviousAttemptScores)] = new OpenApiSchema
                {
                    Type = "string",
                    Nullable = true,
                    Description = "Optional JSON array of previous overall scores, e.g. [40.5,55]."
                },
                [nameof(EvaluateAttemptFormRequest.Language)] = new OpenApiSchema
                {
                    Type = "string"
                },
                [nameof(EvaluateAttemptFormRequest.Age)] = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32"
                },
                [nameof(EvaluateAttemptFormRequest.SessionExecution)] = new OpenApiSchema
                {
                    Type = "string",
                    Nullable = true,
                    Description = "Optional JSON snapshot of sessionExecution from the previous evaluate response."
                }
            }
        };
    }
}
