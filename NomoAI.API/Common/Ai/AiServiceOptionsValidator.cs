using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NomoAI.API.Common.Ai;

public sealed class AiServiceOptionsValidator : IValidateOptions<AiServiceOptions>
{
    private readonly IHostEnvironment _environment;

    public AiServiceOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, AiServiceOptions options)
    {
        var errors = new List<string>();

        bool hasBaseUrl = !string.IsNullOrWhiteSpace(options.BaseUrl);
        bool hasServiceKey = !string.IsNullOrWhiteSpace(options.ServiceKey);

        // Allow fully unconfigured AI so the host can start (Swagger/API still work).
        // AI endpoints fail at call-time when BaseUrl/ServiceKey are missing.
        if (!hasBaseUrl && !hasServiceKey)
        {
            // Still validate numeric bounds below.
        }
        else
        {
            if (!hasBaseUrl)
            {
                errors.Add("AiService:BaseUrl is required when AiService is partially configured.");
            }
            else if (!Uri.TryCreate(options.BaseUrl.Trim(), UriKind.Absolute, out Uri? uri) ||
                     (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add("AiService:BaseUrl must be an absolute HTTP or HTTPS URL.");
            }

            bool isTesting = _environment.IsEnvironment("Testing");
            if (!isTesting && !hasServiceKey)
            {
                errors.Add("AiService:ServiceKey is required outside the Testing environment.");
            }
        }

        if (options.TimeoutSeconds is < 5 or > 600)
        {
            errors.Add("AiService:TimeoutSeconds must be between 5 and 600.");
        }

        if (options.HealthTimeoutSeconds is < 1 or > 60)
        {
            errors.Add("AiService:HealthTimeoutSeconds must be between 1 and 60.");
        }

        if (options.MaxRetryAttempts is < 0 or > 5)
        {
            errors.Add("AiService:MaxRetryAttempts must be between 0 and 5.");
        }

        if (options.MaxAudioBytes is < 1 or > 52_428_800)
        {
            errors.Add("AiService:MaxAudioBytes must be between 1 byte and 50 MB.");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
