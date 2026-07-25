using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;

namespace NomoAI.API.Infrastructure.Ai;

internal static class AiServiceErrorMapper
{
    private static readonly HashSet<string> InsufficientKnowledgeCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "insufficient_approved_knowledge",
        "insufficient_approved_intervention",
        "insufficient_knowledge"
    };

    public static Error MapHttpStatus(
        HttpStatusCode statusCode,
        string? responseBody,
        string? responseCorrelationId,
        string fallbackCorrelationId)
    {
        string? correlationId = responseCorrelationId ?? ExtractCorrelationId(responseBody) ?? fallbackCorrelationId;
        string? remoteCode = ExtractErrorCode(responseBody);

        return statusCode switch
        {
            HttpStatusCode.BadRequest => AiServiceErrors.InvalidRequest(correlationId),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                AiServiceErrors.Unauthorized(correlationId),
            HttpStatusCode.NotFound => AiServiceErrors.NotFound(correlationId),
            HttpStatusCode.UnprocessableEntity => MapUnprocessable(remoteCode, correlationId),
            HttpStatusCode.TooManyRequests => AiServiceErrors.RateLimited(correlationId),
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout => AiServiceErrors.Unavailable(correlationId),
            >= HttpStatusCode.InternalServerError => AiServiceErrors.InternalError(correlationId),
            _ => AiServiceErrors.InvalidResponse(correlationId)
        };
    }

    public static Error MapException(Exception exception, string correlationId)
    {
        return exception switch
        {
            OperationCanceledException or TaskCanceledException =>
                AiServiceErrors.Timeout(correlationId),
            HttpRequestException => AiServiceErrors.Unavailable(correlationId),
            JsonException => AiServiceErrors.InvalidResponse(correlationId),
            _ => AiServiceErrors.Unavailable(correlationId)
        };
    }

    public static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    public static void LogSafeFailure(
        ILogger logger,
        HttpStatusCode? statusCode,
        string operation,
        string correlationId,
        Exception? exception = null)
    {
        if (exception is null)
        {
            logger.LogWarning(
                "AI Core {Operation} failed. StatusCode: {StatusCode}. CorrelationId: {CorrelationId}.",
                operation,
                statusCode.HasValue ? (int)statusCode.Value : (int?)null,
                correlationId);
            return;
        }

        logger.LogWarning(
            exception,
            "AI Core {Operation} failed with exception. CorrelationId: {CorrelationId}.",
            operation,
            correlationId);
    }

    private static Error MapUnprocessable(string? remoteCode, string? correlationId)
    {
        if (remoteCode is not null && InsufficientKnowledgeCodes.Contains(remoteCode))
        {
            return AiServiceErrors.InsufficientKnowledge(correlationId);
        }

        return AiServiceErrors.InsufficientKnowledge(correlationId);
    }

    private static string? ExtractCorrelationId(string? responseBody)
    {
        AiErrorEnvelope? envelope = TryDeserializeError(responseBody);
        return string.IsNullOrWhiteSpace(envelope?.Error?.CorrelationId)
            ? null
            : envelope!.Error!.CorrelationId;
    }

    private static string? ExtractErrorCode(string? responseBody)
    {
        AiErrorEnvelope? envelope = TryDeserializeError(responseBody);
        return string.IsNullOrWhiteSpace(envelope?.Error?.Code)
            ? null
            : envelope!.Error!.Code;
    }

    private static AiErrorEnvelope? TryDeserializeError(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiErrorEnvelope>(
                responseBody,
                AiCoreJsonSerializerOptions.Instance);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
