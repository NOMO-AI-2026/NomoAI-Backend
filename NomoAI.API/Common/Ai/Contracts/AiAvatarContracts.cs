using System.Text.Json.Serialization;

namespace NomoAI.API.Common.Ai.Contracts;

/// <summary>
/// Avatar instruction payload. Emotion, gesture, action, nextStep, and
/// frontendStateAfter are strings so unknown future FastAPI values do not break deserialization.
/// </summary>
public sealed class AiAvatarInstructionDto
{
    [JsonPropertyName("spokenText")]
    public string SpokenText { get; init; } = string.Empty;

    [JsonPropertyName("emotion")]
    public string Emotion { get; init; } = string.Empty;

    [JsonPropertyName("gesture")]
    public string Gesture { get; init; } = string.Empty;

    [JsonPropertyName("speechRate")]
    public double SpeechRate { get; init; }

    [JsonPropertyName("action")]
    public string Action { get; init; } = string.Empty;

    [JsonPropertyName("nextStep")]
    public string NextStep { get; init; } = string.Empty;

    [JsonPropertyName("segments")]
    public IReadOnlyList<AiAvatarSegmentDto> Segments { get; init; } = Array.Empty<AiAvatarSegmentDto>();
}

public sealed class AiAvatarSegmentDto
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("emotion")]
    public string Emotion { get; init; } = string.Empty;

    [JsonPropertyName("gesture")]
    public string Gesture { get; init; } = string.Empty;

    [JsonPropertyName("speechRate")]
    public double SpeechRate { get; init; }

    [JsonPropertyName("pauseAfterMs")]
    public int PauseAfterMs { get; init; }

    [JsonPropertyName("expectedDurationMs")]
    public int? ExpectedDurationMs { get; init; }

    [JsonPropertyName("frontendStateAfter")]
    public string? FrontendStateAfter { get; init; }
}
