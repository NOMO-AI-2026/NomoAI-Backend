using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Common.Ai;

public static class AiServiceErrors
{
    public static Error InvalidRequest(string? correlationId = null) => new(
        "AiService.InvalidRequest",
        "The AI service rejected the request as invalid.",
        StatusCodes.Status400BadRequest,
        correlationId);

    public static Error Unauthorized(string? correlationId = null) => new(
        "AiService.Unauthorized",
        "The AI service rejected the service credentials. Check AiService configuration.",
        StatusCodes.Status502BadGateway,
        correlationId);

    public static Error NotFound(string? correlationId = null) => new(
        "AiService.NotFound",
        "The requested AI resource was not found.",
        StatusCodes.Status404NotFound,
        correlationId);

    public static Error InsufficientKnowledge(string? correlationId = null) => new(
        "AiService.InsufficientKnowledge",
        "The AI service could not complete the request due to missing or insufficient approved knowledge.",
        StatusCodes.Status422UnprocessableEntity,
        correlationId);

    public static Error RateLimited(string? correlationId = null) => new(
        "AiService.RateLimited",
        "The AI service rate-limited the request. Try again shortly.",
        StatusCodes.Status429TooManyRequests,
        correlationId);

    public static Error Unavailable(string? correlationId = null) => new(
        "AiService.Unavailable",
        "The AI service is temporarily unavailable.",
        StatusCodes.Status503ServiceUnavailable,
        correlationId);

    public static Error Timeout(string? correlationId = null) => new(
        "AiService.Timeout",
        "The AI service request timed out.",
        StatusCodes.Status504GatewayTimeout,
        correlationId);

    public static Error InvalidResponse(string? correlationId = null) => new(
        "AiService.InvalidResponse",
        "The AI service returned an invalid or unexpected response.",
        StatusCodes.Status502BadGateway,
        correlationId);

    public static Error InternalError(string? correlationId = null) => new(
        "AiService.InternalError",
        "The AI service reported an internal error.",
        StatusCodes.Status502BadGateway,
        correlationId);

    public static Error AudioInvalid() => new(
        "AiService.AudioInvalid",
        "The uploaded audio file is missing, empty, unsupported, or too large.",
        StatusCodes.Status400BadRequest);
}
