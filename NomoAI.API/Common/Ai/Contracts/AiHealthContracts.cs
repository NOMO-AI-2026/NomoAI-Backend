using System.Text.Json.Serialization;

namespace NomoAI.API.Common.Ai.Contracts;

public sealed class AiHealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("service")]
    public string? Service { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("environment")]
    public string? Environment { get; init; }
}

public sealed class AiReadyResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("service")]
    public string? Service { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("dependencies")]
    public IReadOnlyList<AiDependencyStatus>? Dependencies { get; init; }
}

public sealed class AiDependencyStatus
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("configured")]
    public bool Configured { get; init; }

    [JsonPropertyName("ready")]
    public bool? Ready { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }
}
