using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;

namespace NomoAI.API.Infrastructure.Ai;

public sealed class AiCoreReadinessHealthCheck : IHealthCheck
{
    private readonly IAiCoreClient _aiCoreClient;
    private readonly ILogger<AiCoreReadinessHealthCheck> _logger;

    public AiCoreReadinessHealthCheck(
        IAiCoreClient aiCoreClient,
        ILogger<AiCoreReadinessHealthCheck> logger)
    {
        _aiCoreClient = aiCoreClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await _aiCoreClient.CheckReadinessAsync(cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "AI Core readiness check failed. ErrorCode: {ErrorCode}. CorrelationId: {CorrelationId}.",
                result.Error.Code,
                result.Error.CorrelationId);

            return HealthCheckResult.Unhealthy(
                "AI Core is not ready.",
                data: new Dictionary<string, object>
                {
                    ["errorCode"] = result.Error.Code,
                    ["correlationId"] = result.Error.CorrelationId ?? string.Empty
                });
        }

        AiReadyResponse ready = result.Value;
        bool isReady = string.Equals(ready.Status, "ready", StringComparison.OrdinalIgnoreCase);

        if (!isReady)
        {
            return HealthCheckResult.Degraded(
                "AI Core reported not_ready.",
                data: new Dictionary<string, object>
                {
                    ["status"] = ready.Status
                });
        }

        return HealthCheckResult.Healthy("AI Core is ready.");
    }
}
