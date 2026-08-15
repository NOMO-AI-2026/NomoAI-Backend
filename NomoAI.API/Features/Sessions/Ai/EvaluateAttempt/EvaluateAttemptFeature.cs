using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;

namespace NomoAI.API.Features.Sessions.Ai.EvaluateAttempt;

/// <summary>
/// Multipart form contract for Swagger and Minimal API [FromForm] binding.
/// Property names match the FastAPI form field names (camelCase via default form binding).
/// </summary>
public sealed class EvaluateAttemptFormRequest
{
    public IFormFile? Audio { get; init; }

    public string TargetValue { get; init; } = string.Empty;

    public int ConsecutiveNoSpeechCount { get; init; }

    public string SpeechLevel { get; init; } = string.Empty;

    public int? MaximumAttempts { get; init; }

    public string? AdditionalContext { get; init; }

    public string ActivityType { get; init; } = string.Empty;

    public string ActivityId { get; init; } = string.Empty;

    public int? SessionStepNumber { get; init; }

    public string? SessionId { get; init; }

    public string? PreviousDecision { get; init; }

    public int AttemptNumber { get; init; }

    public string ChildId { get; init; } = string.Empty;

    /// <summary>JSON array string, e.g. [40.5,55].</summary>
    public string? PreviousAttemptScores { get; init; }

    public string Language { get; init; } = "ar";

    public int Age { get; init; }

    /// <summary>Optional JSON snapshot of sessionExecution from the previous evaluate response.</summary>
    public string? SessionExecution { get; init; }
}

/// <summary>
/// MediatR command uses Stream metadata instead of IFormFile to keep handlers transport-agnostic.
/// The endpoint owns opening/disposing the audio stream around Send.
/// </summary>
public sealed record EvaluateAttemptCommand(
    Stream? AudioStream,
    string FileName,
    string ContentType,
    long AudioLengthBytes,
    string ChildId,
    string ActivityId,
    string ActivityType,
    string TargetValue,
    string SpeechLevel,
    int Age,
    int AttemptNumber,
    string? SessionId,
    int? SessionStepNumber,
    int? MaximumAttempts,
    string? PreviousAttemptScoresJson,
    string? PreviousDecision,
    int ConsecutiveNoSpeechCount,
    string Language,
    string? AdditionalContext,
    string? SessionExecutionJson = null) : IRequest<Result<AiEvaluateAttemptResponse>>;

public sealed class EvaluateAttemptCommandValidator : AbstractValidator<EvaluateAttemptCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/wav",
        "audio/x-wav",
        "audio/mpeg",
        "audio/mp3",
        "audio/mp4",
        "audio/x-m4a",
        "audio/m4a",
        "audio/webm",
        "audio/ogg",
        "application/ogg",
        "application/octet-stream"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".mp3", ".m4a", ".webm", ".ogg"
    };

    public EvaluateAttemptCommandValidator(IOptions<AiServiceOptions> options)
    {
        long maxBytes = options.Value.MaxAudioBytes;

        RuleFor(x => x.AudioStream)
            .NotNull()
            .WithMessage("Audio file is required.");

        RuleFor(x => x.AudioLengthBytes)
            .GreaterThan(0)
            .WithMessage("Audio file must not be empty.")
            .LessThanOrEqualTo(maxBytes)
            .WithMessage($"Audio file must not exceed {maxBytes} bytes.");

        RuleFor(x => x.ContentType)
            .Must(BeAllowedContentType)
            .WithMessage("Audio content type is not supported.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(HaveAllowedExtension)
            .WithMessage("Audio file extension is not supported.");

        RuleFor(x => x.ChildId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ActivityId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ActivityType).NotEmpty().MaximumLength(32);
        RuleFor(x => x.TargetValue).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SpeechLevel).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Age).InclusiveBetween(1, 18);
        RuleFor(x => x.AttemptNumber).InclusiveBetween(1, 20);
        RuleFor(x => x.Language).NotEmpty().MaximumLength(16);
        RuleFor(x => x.ConsecutiveNoSpeechCount).InclusiveBetween(0, 20);
        RuleFor(x => x.AdditionalContext).MaximumLength(1000).When(x => x.AdditionalContext is not null);
        RuleFor(x => x.PreviousAttemptScoresJson)
            .Must(BeValidScoresJsonOrEmpty)
            .WithMessage("previousAttemptScores must be a JSON array of numbers between 0 and 100.")
            .When(x => !string.IsNullOrWhiteSpace(x.PreviousAttemptScoresJson));
    }

    private static bool BeAllowedContentType(string? contentType)
    {
        string normalized = (contentType ?? string.Empty).Split(';')[0].Trim();
        return string.IsNullOrWhiteSpace(normalized) || AllowedContentTypes.Contains(normalized);
    }

    private static bool HaveAllowedExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return AllowedExtensions.Contains(Path.GetExtension(fileName));
    }

    private static bool BeValidScoresJsonOrEmpty(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        try
        {
            var scores = System.Text.Json.JsonSerializer.Deserialize<List<double>>(raw);
            return scores is not null && scores.All(score => score is >= 0 and <= 100);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}

/// <summary>
/// Backward-compatible proxy handler. The public HTTP contract still accepts
/// (and echoes back) childId/activityId/sessionId so existing React callers keep
/// working, but the outbound AI Core call uses the V2 evaluate endpoint: no
/// database IDs are sent, and TargetValue is forwarded as "prompt".
/// </summary>
public sealed class EvaluateAttemptCommandHandler
    : IRequestHandler<EvaluateAttemptCommand, Result<AiEvaluateAttemptResponse>>
{
    private readonly IAiCoreClient _aiCoreClient;

    public EvaluateAttemptCommandHandler(IAiCoreClient aiCoreClient)
    {
        _aiCoreClient = aiCoreClient;
    }

    public async Task<Result<AiEvaluateAttemptResponse>> Handle(
        EvaluateAttemptCommand request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<double>? previousScores = ParsePreviousScores(request.PreviousAttemptScoresJson);

        var aiRequest = new AiEvaluateAttemptV2Request
        {
            AudioStream = request.AudioStream!,
            FileName = Path.GetFileName(request.FileName),
            ContentType = string.IsNullOrWhiteSpace(request.ContentType)
                ? "application/octet-stream"
                : request.ContentType.Split(';')[0].Trim(),
            ActivityType = request.ActivityType,
            Prompt = request.TargetValue,
            SpeechLevel = request.SpeechLevel,
            Age = request.Age,
            AttemptNumber = request.AttemptNumber,
            MaximumAttempts = request.MaximumAttempts,
            PreviousAttemptScores = previousScores,
            PreviousDecision = request.PreviousDecision,
            ConsecutiveNoSpeechCount = request.ConsecutiveNoSpeechCount,
            Language = request.Language,
            SessionExecution = ParseSessionExecution(request.SessionExecutionJson)
        };

        Result<AiEvaluateAttemptV2Response> result =
            await _aiCoreClient.EvaluateAttemptAsync(aiRequest, cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<AiEvaluateAttemptResponse>(result.Error);
        }

        return Result.Success(ToLegacyResponse(request, result.Value));
    }

    private static AiEvaluateAttemptResponse ToLegacyResponse(
        EvaluateAttemptCommand request,
        AiEvaluateAttemptV2Response v2) => new()
    {
        SessionId = request.SessionId,
        ActivityId = request.ActivityId,
        AttemptNumber = v2.AttemptNumber,
        ActivityType = v2.ActivityType,
        TargetValue = v2.Prompt,
        SpeechOutcome = v2.SpeechOutcome,
        SpeechAnalysis = ToLegacySpeechAnalysis(v2.SpeechAnalysis),
        AdaptiveDecision = v2.AdaptiveDecision,
        KnowledgeSourceIds = v2.KnowledgeSourceIds,
        Avatar = v2.Avatar,
        AvatarModel = v2.AvatarModel,
        AvatarGenerationMode = v2.AvatarGenerationMode,
        UsedFallback = v2.UsedFallback,
        GeneratedAt = v2.GeneratedAt,
        SessionExecution = v2.SessionExecution
    };

    private static AiSpeechAnalysisResultDto? ToLegacySpeechAnalysis(AiSpeechAnalysisV2Dto? v2) =>
        v2 is null
            ? null
            : new AiSpeechAnalysisResultDto
            {
                ActivityType = v2.ActivityType,
                TargetValue = v2.Prompt,
                Audio = v2.Audio,
                Vad = v2.Vad,
                Transcription = v2.Transcription,
                Normalization = v2.Normalization,
                Scores = v2.Scores is null
                    ? null
                    : new AiScoreBreakdownDto
                    {
                        AccuracyScore = v2.Scores.AccuracyScore ?? 0,
                        CompletenessScore = v2.Scores.CompletenessScore ?? 0,
                        FluencyScore = v2.Scores.FluencyScore,
                        PronunciationProxyScore = v2.Scores.PronunciationProxyScore,
                        OverallScore = v2.Scores.OverallScore ?? 0,
                        Matched = v2.Scores.Matched,
                        ReasonCodes = v2.Scores.ReasonCodes,
                        ScoringMethod = v2.Scores.ScoringMethod,
                        RulesVersion = v2.Scores.RulesVersion
                    }
            };

    private static IReadOnlyList<double>? ParsePreviousScores(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<double>>(raw);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static AiSessionExecutionDto? ParseSessionExecution(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<AiSessionExecutionDto>(
                raw,
                NomoAI.API.Infrastructure.Ai.AiCoreJsonSerializerOptions.Instance);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}

public static class EvaluateAttemptEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/evaluate", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor", "Parent"))
            // Bearer JWT APIs do not use cookie antiforgery. Disable only on this
            // multipart Minimal API endpoint (not globally). See docs/ai-core-integration.md.
            .DisableAntiforgery()
            .WithName("EvaluateAttemptAi")
            .WithSummary("Evaluate an audio attempt via AI Core")
            .WithDescription(
                "Accepts multipart/form-data with a required audio file and attempt metadata. " +
                "Forwards the audio stream to the AI Core evaluate endpoint.")
            .Produces<AiEvaluateAttemptResponse>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status422UnprocessableEntity)
            .Produces<Error>(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleAsync(
        [FromForm] EvaluateAttemptFormRequest request,
        ISender sender,
        CancellationToken cancellationToken = default)
    {
        string fileName = request.Audio is null
            ? string.Empty
            : Path.GetFileName(request.Audio.FileName);

        string contentType = request.Audio is null
            ? string.Empty
            : (request.Audio.ContentType ?? "application/octet-stream").Split(';')[0].Trim();

        long length = request.Audio?.Length ?? 0;

        // Endpoint owns the stream lifetime for the duration of the MediatR call.
        await using Stream? audioStream = request.Audio?.OpenReadStream();

        var command = new EvaluateAttemptCommand(
            audioStream,
            fileName,
            contentType,
            length,
            request.ChildId,
            request.ActivityId,
            request.ActivityType,
            request.TargetValue,
            request.SpeechLevel,
            request.Age,
            request.AttemptNumber,
            request.SessionId,
            request.SessionStepNumber,
            request.MaximumAttempts,
            request.PreviousAttemptScores,
            request.PreviousDecision,
            request.ConsecutiveNoSpeechCount,
            request.Language,
            request.AdditionalContext,
            request.SessionExecution);

        Result<AiEvaluateAttemptResponse> result =
            await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(
                result.Error,
                statusCode: result.Error.StatusCode);
        }

        return Results.Ok(result.Value);
    }
}
