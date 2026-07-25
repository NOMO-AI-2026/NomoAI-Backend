using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Infrastructure.Ai;

namespace NomoAI.API.Tests;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Handler { get; set; } =
        (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Buffer content so tests can inspect multipart after send.
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync();
        }

        Requests.Add(request);
        return await Handler(request, cancellationToken);
    }
}

public sealed class TestCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly string _value;

    public TestCorrelationIdAccessor(string value = "corr-test-001")
    {
        _value = value;
    }

    public string GetOrCreate() => _value;
}

public sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "NomoAI.API.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}

internal static class AiCoreClientTestFactory
{
    public static (AiCoreClient Client, FakeHttpMessageHandler Handler, ListLogger<AiCoreClient> Logger)
        Create(
            AiServiceOptions? options = null,
            string correlationId = "corr-test-001")
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://ai-core.test/")
        };

        options ??= new AiServiceOptions
        {
            BaseUrl = "http://ai-core.test/",
            ServiceKey = "test-service-key",
            TimeoutSeconds = 30,
            HealthTimeoutSeconds = 5,
            MaxRetryAttempts = 2
        };

        var logger = new ListLogger<AiCoreClient>();
        var client = new AiCoreClient(
            httpClient,
            Options.Create(options),
            new TestCorrelationIdAccessor(correlationId),
            logger);

        return (client, handler, logger);
    }
}

public sealed class ListLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
    }
}
