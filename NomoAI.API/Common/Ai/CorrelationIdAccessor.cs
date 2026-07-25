using Microsoft.AspNetCore.Http;

namespace NomoAI.API.Common.Ai;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private const string ItemsKey = "__nomoai.correlation_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetOrCreate()
    {
        HttpContext? context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return CreateNewId();
        }

        if (context.Items.TryGetValue(ItemsKey, out object? existing) &&
            existing is string cached &&
            !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        string? fromHeader = context.Request.Headers[AiServiceOptions.CorrelationIdHeaderName]
            .FirstOrDefault();

        string correlationId = Normalize(fromHeader) ?? CreateNewId();
        context.Items[ItemsKey] = correlationId;

        if (!context.Response.HasStarted &&
            !context.Response.Headers.ContainsKey(AiServiceOptions.CorrelationIdHeaderName))
        {
            context.Response.Headers[AiServiceOptions.CorrelationIdHeaderName] = correlationId;
        }

        return correlationId;
    }

    private static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        string trimmed = raw.Trim();
        if (trimmed.Length > 128)
        {
            trimmed = trimmed[..128];
        }

        return trimmed;
    }

    private static string CreateNewId() => Guid.NewGuid().ToString("N");
}
