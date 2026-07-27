namespace NomoAI.API.Common.Ai;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    public const string ServiceKeyHeaderName = "X-AI-Service-Key";

    public const string CorrelationIdHeaderName = "X-Correlation-ID";

    /// <summary>Maximum audio upload size forwarded to AI Core (matches FastAPI default 10 MB).</summary>
    public const long DefaultMaxAudioBytes = 10_485_760;

    public string BaseUrl { get; init; } = string.Empty;

    public string ServiceKey { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 180;

    public int HealthTimeoutSeconds { get; init; } = 10;

    public int MaxRetryAttempts { get; init; } = 2;

    public long MaxAudioBytes { get; init; } = DefaultMaxAudioBytes;
}
