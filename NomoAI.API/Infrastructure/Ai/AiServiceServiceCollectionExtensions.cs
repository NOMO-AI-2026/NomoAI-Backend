using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Ai;

namespace NomoAI.API.Infrastructure.Ai;

public static class AiServiceServiceCollectionExtensions
{
    public static IServiceCollection AddAiService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<AiServiceOptions>()
            .Bind(configuration.GetSection(AiServiceOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AiServiceOptions>, AiServiceOptionsValidator>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

        services.AddHttpClient<IAiCoreClient, AiCoreClient>((sp, client) =>
            {
                AiServiceOptions options = sp.GetRequiredService<IOptions<AiServiceOptions>>().Value;
                client.BaseAddress = AiServiceBaseUrlNormalizer.CreateBaseAddress(options.BaseUrl);
                // Per-request timeouts are enforced via CancellationTokenSource in AiCoreClient.
                // TLS certificate validation remains enabled (default HttpClient handler).
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
            });

        services.AddHealthChecks()
            .AddCheck<AiCoreReadinessHealthCheck>(
                "ai-core",
                tags: ["ready", "ai"]);

        return services;
    }
}
