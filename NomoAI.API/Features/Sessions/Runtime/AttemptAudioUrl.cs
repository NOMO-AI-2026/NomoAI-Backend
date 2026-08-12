using Microsoft.Extensions.Options;
using NomoAI.API.Common.Options;

namespace NomoAI.API.Features.Sessions.Runtime;

/// <summary>
/// Builds the stable, auth-gated API path used as SessionAttempts.AudioUrl.
/// </summary>
public static class AttemptAudioUrl
{
    public static string BuildRelativePath(int sessionId, int attemptId) =>
        $"/api/sessions/{sessionId}/attempts/{attemptId}/audio";

    public static string Build(int sessionId, int attemptId, string? publicApiBaseUrl)
    {
        string relative = BuildRelativePath(sessionId, attemptId);
        if (string.IsNullOrWhiteSpace(publicApiBaseUrl))
        {
            return relative;
        }

        return $"{publicApiBaseUrl.TrimEnd('/')}{relative}";
    }

    public static string Build(int sessionId, int attemptId, IOptions<PublicApiOptions> options) =>
        Build(sessionId, attemptId, options.Value.BaseUrl);
}
