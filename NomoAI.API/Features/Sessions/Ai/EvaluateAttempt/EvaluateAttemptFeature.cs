using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;

namespace NomoAI.API.Features.Sessions.Ai.EvaluateAttempt;

public sealed record EvaluateAttemptCommand(
    IFormFile Audio,
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
    string? AdditionalContext) : IRequest<Result<AiEvaluateAttemptResponse>>;

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

        RuleFor(x => x.Audio)
            .NotNull()
            .WithMessage("Audio file is required.")
            .Must(file => file is { Length: > 0 })
            .WithMessage("Audio file must not be empty.")
            .Must(file => file is null || file.Length <= maxBytes)
            .WithMessage($"Audio file must not exceed {maxBytes} bytes.")
            .Must(HaveAllowedContentType)
            .WithMessage("Audio content type is not supported.")
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
    }

    private static bool HaveAllowedContentType(IFormFile? file)
    {
        if (file is null)
        {
            return false;
        }

        string contentType = (file.ContentType ?? string.Empty).Split(';')[0].Trim();
        return string.IsNullOrWhiteSpace(contentType) || AllowedContentTypes.Contains(contentType);
    }

    private static bool HaveAllowedExtension(IFormFile? file)
    {
        if (file is null)
        {
            return false;
        }

        string extension = Path.GetExtension(file.FileName);
        return AllowedExtensions.Contains(extension);
    }
}

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

        await using Stream audioStream = request.Audio.OpenReadStream();

        var aiRequest = new AiEvaluateAttemptRequest
        {
            AudioStream = audioStream,
            FileName = Path.GetFileName(request.Audio.FileName),
            ContentType = (request.Audio.ContentType ?? "application/octet-stream")
                .Split(';')[0]
                .Trim(),
            ChildId = request.ChildId,
            ActivityId = request.ActivityId,
            ActivityType = request.ActivityType,
            TargetValue = request.TargetValue,
            SpeechLevel = request.SpeechLevel,
            Age = request.Age,
            AttemptNumber = request.AttemptNumber,
            SessionId = request.SessionId,
            SessionStepNumber = request.SessionStepNumber,
            MaximumAttempts = request.MaximumAttempts,
            PreviousAttemptScores = previousScores,
            PreviousDecision = request.PreviousDecision,
            ConsecutiveNoSpeechCount = request.ConsecutiveNoSpeechCount,
            Language = request.Language,
            AdditionalContext = request.AdditionalContext
        };

        return await _aiCoreClient.EvaluateAttemptAsync(aiRequest, cancellationToken);
    }

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
}

/// <summary>
/// Single multipart form model so Swashbuckle can document file + fields together.
/// Do not bind IFormFile with separate [FromForm] parameters.
/// </summary>
public sealed class EvaluateAttemptFormRequest
{
    public IFormFile Audio { get; init; } = null!;

    public string ChildId { get; init; } = string.Empty;

    public string ActivityId { get; init; } = string.Empty;

    public string ActivityType { get; init; } = string.Empty;

    public string TargetValue { get; init; } = string.Empty;

    public string SpeechLevel { get; init; } = string.Empty;

    public int Age { get; init; }

    public int AttemptNumber { get; init; }

    public string? SessionId { get; init; }

    public int? SessionStepNumber { get; init; }

    public int? MaximumAttempts { get; init; }

    public string? PreviousAttemptScores { get; init; }

    public string? PreviousDecision { get; init; }

    public int ConsecutiveNoSpeechCount { get; init; }

    public string Language { get; init; } = "ar";

    public string? AdditionalContext { get; init; }
}

public static class EvaluateAttemptEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/evaluate", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor", "Parent"))
            .DisableAntiforgery()
            .WithName("EvaluateAttemptAi")
            .WithSummary("Evaluate an audio attempt via AI Core")
            .Accepts<EvaluateAttemptFormRequest>("multipart/form-data")
            .Produces<AiEvaluateAttemptResponse>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status422UnprocessableEntity)
            .Produces<Error>(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleAsync(
        [FromForm] EvaluateAttemptFormRequest form,
        ISender sender,
        CancellationToken cancellationToken = default)
    {
        var command = new EvaluateAttemptCommand(
            form.Audio,
            form.ChildId,
            form.ActivityId,
            form.ActivityType,
            form.TargetValue,
            form.SpeechLevel,
            form.Age,
            form.AttemptNumber,
            form.SessionId,
            form.SessionStepNumber,
            form.MaximumAttempts,
            form.PreviousAttemptScores,
            form.PreviousDecision,
            form.ConsecutiveNoSpeechCount,
            form.Language,
            form.AdditionalContext);

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
