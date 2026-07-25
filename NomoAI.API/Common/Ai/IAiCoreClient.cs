using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai.Contracts;

namespace NomoAI.API.Common.Ai;

public interface IAiCoreClient
{
    Task<Result<AiHealthResponse>> CheckHealthAsync(
        CancellationToken cancellationToken = default);

    Task<Result<AiReadyResponse>> CheckReadinessAsync(
        CancellationToken cancellationToken = default);

    Task<Result<AiSessionPlanResponse>> PlanSessionAsync(
        AiSessionPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AiEvaluateAttemptResponse>> EvaluateAttemptAsync(
        AiEvaluateAttemptRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AiSessionSummaryResponse>> CreateSessionSummaryAsync(
        AiSessionSummaryRequest request,
        CancellationToken cancellationToken = default);
}
