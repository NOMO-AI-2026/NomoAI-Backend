using System.Text.Json;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Infrastructure.Ai;

namespace NomoAI.API.Features.Sessions.Runtime;

/// <summary>
/// Reconstructs FastAPI sessionExecution from the last persisted AttemptEvaluation.EvaluationJson.
/// No extra DB column: the full evaluate snapshot already stores the object.
/// </summary>
internal static class SessionExecutionSnapshot
{
    public static AiSessionExecutionDto? TryReadFromEvaluationJson(string? evaluationJson)
    {
        if (string.IsNullOrWhiteSpace(evaluationJson))
        {
            return null;
        }

        try
        {
            AiEvaluateAttemptV2Response? snapshot = JsonSerializer.Deserialize<AiEvaluateAttemptV2Response>(
                evaluationJson,
                AiCoreJsonSerializerOptions.Instance);

            return snapshot?.SessionExecution;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static AiEvaluateAttemptV2Response? TryDeserializeEvaluation(string? evaluationJson)
    {
        if (string.IsNullOrWhiteSpace(evaluationJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiEvaluateAttemptV2Response>(
                evaluationJson,
                AiCoreJsonSerializerOptions.Instance);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
