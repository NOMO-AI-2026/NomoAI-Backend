using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;

namespace NomoAI.API.Infrastructure.Ai;

public sealed class AiCoreClient : IAiCoreClient
{
    private static readonly MediaTypeHeaderValue JsonMediaType =
        new("application/json") { CharSet = "utf-8" };

    private readonly HttpClient _httpClient;
    private readonly AiServiceOptions _options;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<AiCoreClient> _logger;

    public AiCoreClient(
        HttpClient httpClient,
        IOptions<AiServiceOptions> options,
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<AiCoreClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }

    public Task<Result<AiHealthResponse>> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<AiHealthResponse>(
            HttpMethod.Get,
            "/health",
            body: null,
            requireServiceKey: false,
            operationName: "CheckHealth",
            useHealthTimeout: true,
            allowRetry: true,
            cancellationToken);
    }

    public Task<Result<AiReadyResponse>> CheckReadinessAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<AiReadyResponse>(
            HttpMethod.Get,
            "/ready",
            body: null,
            requireServiceKey: true,
            operationName: "CheckReadiness",
            useHealthTimeout: true,
            allowRetry: true,
            cancellationToken);
    }

    public Task<Result<AiSessionPlanV2Response>> PlanSessionAsync(
        AiSessionPlanV2Request request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendAsync<AiSessionPlanV2Response>(
            HttpMethod.Post,
            "/api/v2/sessions/plan",
            body: request,
            requireServiceKey: true,
            operationName: "PlanSession",
            useHealthTimeout: false,
            allowRetry: true,
            cancellationToken);
    }

    public async Task<Result<AiEvaluateAttemptV2Response>> EvaluateAttemptAsync(
        AiEvaluateAttemptV2Request request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.AudioStream);

        // Multipart audio streams are not safely replayable; do not auto-retry.
        string correlationId = _correlationIdAccessor.GetOrCreate();

        using var content = BuildMultipartContent(request);
        using var httpRequest = CreateRequest(
            HttpMethod.Post,
            "/api/v2/sessions/attempts/evaluate",
            content,
            requireServiceKey: true,
            correlationId);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            using HttpResponseMessage response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            return await ReadResponseAsync<AiEvaluateAttemptV2Response>(
                response,
                "EvaluateAttempt",
                correlationId,
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ((ex is TaskCanceledException or OperationCanceledException) &&
             !cancellationToken.IsCancellationRequested) ||
            ex is JsonException)
        {
            AiServiceErrorMapper.LogSafeFailure(_logger, null, "EvaluateAttempt", correlationId, ex);
            return Result.Failure<AiEvaluateAttemptV2Response>(
                AiServiceErrorMapper.MapException(ex, correlationId));
        }
    }

    public Task<Result<AiSessionSummaryResponse>> CreateSessionSummaryAsync(
        AiSessionSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendAsync<AiSessionSummaryResponse>(
            HttpMethod.Post,
            "/api/v1/sessions/summary",
            body: request,
            requireServiceKey: true,
            operationName: "CreateSessionSummary",
            useHealthTimeout: false,
            allowRetry: true,
            cancellationToken);
    }

    public async Task<Result<AiSpeechAudioResult>> SynthesizeSpeechAsync(
        string text,
        string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<AiSpeechAudioResult>(
                AiServiceErrors.InvalidRequest(_correlationIdAccessor.GetOrCreate()));
        }

        var synthesizeRequest = new AiSynthesizeSpeechRequest
        {
            Text = text,
            Purpose = purpose,
            // Gemini TTS rejects mp3 (HTTP 400). FastAPI converts wav→pcm for Gemini
            // then returns a browser-playable WAV. Do not rely on VPS env default.
            Format = "wav"
        };

        Result<AiSynthesizeSpeechResponse> synthesizeResult = await SendAsync<AiSynthesizeSpeechResponse>(
            HttpMethod.Post,
            "/api/v1/speech/synthesize",
            body: synthesizeRequest,
            requireServiceKey: true,
            operationName: "SynthesizeSpeech",
            useHealthTimeout: false,
            allowRetry: true,
            cancellationToken);

        if (synthesizeResult.IsFailure)
        {
            return Result.Failure<AiSpeechAudioResult>(synthesizeResult.Error);
        }

        string audioId = synthesizeResult.Value.AudioId;
        string correlationId = _correlationIdAccessor.GetOrCreate();

        using var httpRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/v1/speech/audio/{Uri.EscapeDataString(audioId)}",
            content: null,
            requireServiceKey: true,
            correlationId);

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                AiServiceErrorMapper.LogSafeFailure(_logger, response.StatusCode, "SynthesizeSpeech.Download", correlationId);
                response.Dispose();
                return Result.Failure<AiSpeechAudioResult>(
                    AiServiceErrorMapper.MapHttpStatus(response.StatusCode, errorBody, null, correlationId));
            }

            Stream audioStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            string contentType = response.Content.Headers.ContentType?.MediaType ?? synthesizeResult.Value.ContentType;

            return Result.Success(new AiSpeechAudioResult
            {
                Content = audioStream,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/mpeg" : contentType,
                AudioId = audioId
            });
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ((ex is TaskCanceledException or OperationCanceledException) &&
             !cancellationToken.IsCancellationRequested))
        {
            AiServiceErrorMapper.LogSafeFailure(_logger, null, "SynthesizeSpeech.Download", correlationId, ex);
            return Result.Failure<AiSpeechAudioResult>(AiServiceErrorMapper.MapException(ex, correlationId));
        }
    }

    private async Task<Result<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        bool requireServiceKey,
        string operationName,
        bool useHealthTimeout,
        bool allowRetry,
        CancellationToken cancellationToken)
    {
        string correlationId = _correlationIdAccessor.GetOrCreate();

        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            _httpClient.BaseAddress is null)
        {
            return Result.Failure<T>(AiServiceErrors.Unavailable(correlationId));
        }

        int maxAttempts = allowRetry ? Math.Max(1, _options.MaxRetryAttempts + 1) : 1;
        byte[]? bodyBytes = body is null
            ? null
            : JsonSerializer.SerializeToUtf8Bytes(body, AiCoreJsonSerializerOptions.Instance);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            int timeoutSeconds = useHealthTimeout
                ? _options.HealthTimeoutSeconds
                : _options.TimeoutSeconds;
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            HttpContent? content = null;
            if (bodyBytes is not null)
            {
                content = new ByteArrayContent(bodyBytes);
                content.Headers.ContentType = JsonMediaType;
            }

            using var httpRequest = CreateRequest(
                method,
                path,
                content,
                requireServiceKey,
                correlationId);

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);

                if (allowRetry &&
                    attempt < maxAttempts &&
                    AiServiceErrorMapper.IsTransient(response.StatusCode))
                {
                    AiServiceErrorMapper.LogSafeFailure(
                        _logger,
                        response.StatusCode,
                        operationName,
                        correlationId);

                    await Task.Delay(GetRetryDelay(attempt), cancellationToken);
                    continue;
                }

                return await ReadResponseAsync<T>(
                    response,
                    operationName,
                    correlationId,
                    cancellationToken);
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ((ex is TaskCanceledException or OperationCanceledException) &&
                 !cancellationToken.IsCancellationRequested))
            {
                AiServiceErrorMapper.LogSafeFailure(_logger, null, operationName, correlationId, ex);

                if (allowRetry && attempt < maxAttempts)
                {
                    await Task.Delay(GetRetryDelay(attempt), cancellationToken);
                    continue;
                }

                return Result.Failure<T>(AiServiceErrorMapper.MapException(ex, correlationId));
            }
            catch (JsonException ex)
            {
                AiServiceErrorMapper.LogSafeFailure(_logger, null, operationName, correlationId, ex);
                return Result.Failure<T>(AiServiceErrors.InvalidResponse(correlationId));
            }
        }

        return Result.Failure<T>(AiServiceErrors.Unavailable(correlationId));
    }

    private async Task<Result<T>> ReadResponseAsync<T>(
        HttpResponseMessage response,
        string operationName,
        string requestCorrelationId,
        CancellationToken cancellationToken)
    {
        string? responseCorrelationId = response.Headers.TryGetValues(
                AiServiceOptions.CorrelationIdHeaderName,
                out IEnumerable<string>? values)
            ? values.FirstOrDefault()
            : null;

        string correlationId = string.IsNullOrWhiteSpace(responseCorrelationId)
            ? requestCorrelationId
            : responseCorrelationId!;

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            AiServiceErrorMapper.LogSafeFailure(
                _logger,
                response.StatusCode,
                operationName,
                correlationId);

            return Result.Failure<T>(
                AiServiceErrorMapper.MapHttpStatus(
                    response.StatusCode,
                    errorBody,
                    responseCorrelationId,
                    requestCorrelationId));
        }

        try
        {
            T? payload = await response.Content.ReadFromJsonAsync<T>(
                AiCoreJsonSerializerOptions.Instance,
                cancellationToken);

            if (payload is null)
            {
                AiServiceErrorMapper.LogSafeFailure(
                    _logger,
                    response.StatusCode,
                    operationName,
                    correlationId);

                return Result.Failure<T>(AiServiceErrors.InvalidResponse(correlationId));
            }

            _logger.LogInformation(
                "AI Core {Operation} succeeded. StatusCode: {StatusCode}. CorrelationId: {CorrelationId}.",
                operationName,
                (int)response.StatusCode,
                correlationId);

            return Result.Success(payload);
        }
        catch (JsonException ex)
        {
            AiServiceErrorMapper.LogSafeFailure(_logger, response.StatusCode, operationName, correlationId, ex);
            return Result.Failure<T>(AiServiceErrors.InvalidResponse(correlationId));
        }
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        HttpContent? content,
        bool requireServiceKey,
        string correlationId)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = content
        };

        request.Headers.TryAddWithoutValidation(
            AiServiceOptions.CorrelationIdHeaderName,
            correlationId);

        if (requireServiceKey)
        {
            request.Headers.TryAddWithoutValidation(
                AiServiceOptions.ServiceKeyHeaderName,
                _options.ServiceKey);
        }

        return request;
    }

    internal static MultipartFormDataContent BuildMultipartContent(AiEvaluateAttemptV2Request request)
    {
        var content = new MultipartFormDataContent();

        var audioContent = new StreamContent(request.AudioStream);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(request.ContentType)
                ? "application/octet-stream"
                : request.ContentType);

        content.Add(audioContent, "audio", request.FileName);
        content.Add(new StringContent(request.ActivityType, Encoding.UTF8), "activityType");
        content.Add(new StringContent(request.Prompt, Encoding.UTF8), "prompt");
        content.Add(new StringContent(request.SpeechLevel, Encoding.UTF8), "speechLevel");
        content.Add(new StringContent(request.Age.ToString(), Encoding.UTF8), "age");
        content.Add(new StringContent(request.AttemptNumber.ToString(), Encoding.UTF8), "attemptNumber");
        content.Add(
            new StringContent(request.ConsecutiveNoSpeechCount.ToString(), Encoding.UTF8),
            "consecutiveNoSpeechCount");
        content.Add(new StringContent(request.Language, Encoding.UTF8), "language");

        if (request.MaximumAttempts is int maxAttempts)
        {
            content.Add(new StringContent(maxAttempts.ToString(), Encoding.UTF8), "maximumAttempts");
        }

        if (request.PreviousAttemptScores is { Count: > 0 })
        {
            string scoresJson = JsonSerializer.Serialize(
                request.PreviousAttemptScores,
                AiCoreJsonSerializerOptions.Instance);
            content.Add(new StringContent(scoresJson, Encoding.UTF8), "previousAttemptScores");
        }

        if (!string.IsNullOrWhiteSpace(request.PreviousDecision))
        {
            content.Add(new StringContent(request.PreviousDecision, Encoding.UTF8), "previousDecision");
        }

        return content;
    }

    private static TimeSpan GetRetryDelay(int attemptNumber) =>
        TimeSpan.FromMilliseconds(200 * Math.Pow(2, attemptNumber - 1));
}
