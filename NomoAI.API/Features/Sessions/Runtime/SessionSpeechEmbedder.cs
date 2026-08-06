using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;

namespace NomoAI.API.Features.Sessions.Runtime;

/// <summary>
/// Best-effort TTS buffering so runtime responses can carry audio inline and
/// eliminate a follow-up GET .../speech round-trip from the React client.
/// </summary>
internal static class SessionSpeechEmbedder
{
    public static async Task<(string? Base64, string? ContentType)> TryBufferAsync(
        IAiCoreClient aiCoreClient,
        string? spokenText,
        string purpose,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(spokenText))
        {
            return (null, null);
        }

        try
        {
            Result<AiSpeechAudioResult> speechResult =
                await aiCoreClient.SynthesizeSpeechAsync(spokenText, purpose, cancellationToken);

            if (speechResult.IsFailure)
            {
                return (null, null);
            }

            using AiSpeechAudioResult audio = speechResult.Value;
            await using var buffer = new MemoryStream();
            await audio.Content.CopyToAsync(buffer, cancellationToken);
            return (Convert.ToBase64String(buffer.ToArray()), audio.ContentType);
        }
        catch
        {
            return (null, null);
        }
    }
}
