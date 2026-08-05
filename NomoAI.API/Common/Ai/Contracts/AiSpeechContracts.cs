using System.Text.Json.Serialization;

namespace NomoAI.API.Common.Ai.Contracts;

public sealed class AiSynthesizeSpeechRequest
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("purpose")]
    public string? Purpose { get; init; }

    [JsonPropertyName("voice")]
    public string? Voice { get; init; }

    [JsonPropertyName("format")]
    public string? Format { get; init; }

    [JsonPropertyName("speed")]
    public double? Speed { get; init; }
}

public sealed class AiSynthesizeSpeechResponse
{
    [JsonPropertyName("audioId")]
    public string AudioId { get; init; } = string.Empty;

    [JsonPropertyName("audioUrl")]
    public string AudioUrl { get; init; } = string.Empty;

    [JsonPropertyName("contentType")]
    public string ContentType { get; init; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; init; } = string.Empty;

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("cached")]
    public bool Cached { get; init; }

    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("voice")]
    public string Voice { get; init; } = string.Empty;

    [JsonPropertyName("durationMs")]
    public int? DurationMs { get; init; }

    [JsonPropertyName("languagePreference")]
    public string? LanguagePreference { get; init; }

    [JsonPropertyName("dialectPreference")]
    public string? DialectPreference { get; init; }
}

/// <summary>
/// Client-side result of a synthesize-then-download round trip. Not a wire DTO:
/// <see cref="Content"/> is an open, forward-only stream the caller must dispose.
/// </summary>
public sealed class AiSpeechAudioResult : IDisposable
{
    public required Stream Content { get; init; }

    public required string ContentType { get; init; }

    public string? AudioId { get; init; }

    public void Dispose() => Content.Dispose();
}
