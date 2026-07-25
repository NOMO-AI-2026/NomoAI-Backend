using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Ai;

namespace NomoAI.API.Tests;

public class AiServiceOptionsValidatorTests
{
    [Fact]
    public void Validate_Accepts_Valid_Absolute_Http_Url_And_ServiceKey()
    {
        var validator = new AiServiceOptionsValidator(new TestHostEnvironment
        {
            EnvironmentName = Environments.Development
        });

        var result = validator.Validate(null, new AiServiceOptions
        {
            BaseUrl = "http://localhost:8000",
            ServiceKey = "local-development-secret",
            TimeoutSeconds = 120,
            HealthTimeoutSeconds = 10,
            MaxRetryAttempts = 2
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_Requires_ServiceKey_Outside_Testing()
    {
        var validator = new AiServiceOptionsValidator(new TestHostEnvironment
        {
            EnvironmentName = Environments.Development
        });

        var result = validator.Validate(null, new AiServiceOptions
        {
            BaseUrl = "https://ai.example.com",
            ServiceKey = "",
            TimeoutSeconds = 60,
            HealthTimeoutSeconds = 5,
            MaxRetryAttempts = 1
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("ServiceKey"));
    }

    [Fact]
    public void Validate_Allows_Empty_ServiceKey_In_Testing()
    {
        var validator = new AiServiceOptionsValidator(new TestHostEnvironment
        {
            EnvironmentName = "Testing"
        });

        var result = validator.Validate(null, new AiServiceOptions
        {
            BaseUrl = "http://localhost:8000",
            ServiceKey = "",
            TimeoutSeconds = 60,
            HealthTimeoutSeconds = 5,
            MaxRetryAttempts = 1
        });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://localhost:8000")]
    [InlineData("")]
    public void Validate_Rejects_Invalid_BaseUrl(string baseUrl)
    {
        var validator = new AiServiceOptionsValidator(new TestHostEnvironment
        {
            EnvironmentName = "Testing"
        });

        var result = validator.Validate(null, new AiServiceOptions
        {
            BaseUrl = baseUrl,
            ServiceKey = "x",
            TimeoutSeconds = 60,
            HealthTimeoutSeconds = 5,
            MaxRetryAttempts = 1
        });

        Assert.True(result.Failed);
    }
}
