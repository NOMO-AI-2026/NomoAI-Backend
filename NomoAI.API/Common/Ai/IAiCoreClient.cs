using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai.Contracts;

namespace NomoAI.API.Common.Ai;

public interface IAiCoreClient
{
    Task<Result<AiHealthResponse>> CheckHealthAsync(
        CancellationToken cancellationToken = default);

    Task<Result<AiReadyResponse>> CheckReadinessAsync(
        CancellationToken cancellationToken = default);

    /// <summary>POST /api/v2/sessions/plan — therapy content only, no database IDs.</summary>
    Task<Result<AiSessionPlanV2Response>> PlanSessionAsync(
        AiSessionPlanV2Request request,
        CancellationToken cancellationToken = default);

    /// <summary>POST /api/v2/sessions/attempts/evaluate — multipart, no database IDs.</summary>
    Task<Result<AiEvaluateAttemptV2Response>> EvaluateAttemptAsync(
        AiEvaluateAttemptV2Request request,
        CancellationToken cancellationToken = default);

    Task<Result<AiSessionSummaryResponse>> CreateSessionSummaryAsync(
        AiSessionSummaryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synthesizes speech via POST /api/v1/speech/synthesize then downloads the
    /// resulting audio via GET /api/v1/speech/audio/{audioId}. The returned stream
    /// is owned by the caller and must be disposed.
    /// </summary>
    Task<Result<AiSpeechAudioResult>> SynthesizeSpeechAsync(
        string text,
        string? purpose = null,
        CancellationToken cancellationToken = default);
}
