namespace NomoAI.API.Common.Ai;

/// <summary>
/// Normalizes AiService:BaseUrl for HttpClient.BaseAddress.
/// Paths such as /api/v1/sessions/plan are supplied by IAiCoreClient methods;
/// BaseUrl must be the host root only (no /api/v1 suffix).
/// </summary>
public static class AiServiceBaseUrlNormalizer
{
    public static Uri CreateBaseAddress(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("AiService:BaseUrl is required.", nameof(baseUrl));
        }

        string trimmed = baseUrl.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "AiService:BaseUrl must be an absolute HTTP or HTTPS URL.",
                nameof(baseUrl));
        }

        // Ensure a trailing slash so absolute-path requests (/api/v1/...) resolve on the host root.
        string root = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/";
        return new Uri(root, UriKind.Absolute);
    }

    public static Uri Combine(string baseUrl, string relativePath)
    {
        Uri baseAddress = CreateBaseAddress(baseUrl);
        string path = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        return new Uri(baseAddress, path);
    }
}
