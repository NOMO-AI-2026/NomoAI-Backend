using System.Text.Json.Serialization;

namespace NomoAI.API.Common.Ai.Contracts;

public sealed class AiErrorEnvelope
{
    [JsonPropertyName("error")]
    public AiErrorBody? Error { get; init; }
}

public sealed class AiErrorBody
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }

    [JsonPropertyName("details")]
    public Dictionary<string, object?>? Details { get; init; }
}
