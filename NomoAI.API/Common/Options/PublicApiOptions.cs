namespace NomoAI.API.Common.Options;

/// <summary>
/// Public-facing base URL for this ASP.NET Core API. Bound from the root
/// "PublicApiBaseUrl" configuration string (not a nested section), so it can be
/// left empty in environments where absolute links are not required.
/// </summary>
public sealed class PublicApiOptions
{
    public const string ConfigKey = "PublicApiBaseUrl";

    public string BaseUrl { get; set; } = string.Empty;
}
