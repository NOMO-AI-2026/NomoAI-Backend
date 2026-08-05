namespace NomoAI.API.Common.Options;

/// <summary>
/// Explicit CORS allow-list bound from the "Cors" configuration section.
/// When <see cref="AllowedOrigins"/> is empty, Development falls back to
/// AllowAnyOrigin for local DX; non-Development environments require explicit origins.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
