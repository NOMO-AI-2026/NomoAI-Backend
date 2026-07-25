using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;

namespace NomoAI.API.Features.Sessions.Ai.CreateSessionSummary;

public sealed record CreateSessionSummaryCommand(
    string SessionId,
    string ActivityId,
    string ActivityType,
    string TargetValue,
    string SpeechLevel,
    int Age,
    string Language,
    IReadOnlyList<AiSummaryAttemptInput> Attempts,
    string? ChildId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int? MaximumAttempts,
    IReadOnlyList<Guid>? SessionPlanSourceIds,
    string? AdditionalContext) : IRequest<Result<AiSessionSummaryResponse>>;

public sealed class CreateSessionSummaryCommandValidator : AbstractValidator<CreateSessionSummaryCommand>
{
    public CreateSessionSummaryCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ActivityId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ActivityType).NotEmpty().MaximumLength(32);
        RuleFor(x => x.TargetValue).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SpeechLevel).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Age).InclusiveBetween(1, 18);
        RuleFor(x => x.Language).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Attempts).NotEmpty().Must(a => a.Count <= 20);
        RuleFor(x => x.AdditionalContext).MaximumLength(1000).When(x => x.AdditionalContext is not null);
    }
}

public sealed class CreateSessionSummaryCommandHandler
    : IRequestHandler<CreateSessionSummaryCommand, Result<AiSessionSummaryResponse>>
{
    private readonly IAiCoreClient _aiCoreClient;

    public CreateSessionSummaryCommandHandler(IAiCoreClient aiCoreClient)
    {
        _aiCoreClient = aiCoreClient;
    }

    public Task<Result<AiSessionSummaryResponse>> Handle(
        CreateSessionSummaryCommand request,
        CancellationToken cancellationToken)
    {
        var aiRequest = new AiSessionSummaryRequest
        {
            SessionId = request.SessionId,
            ActivityId = request.ActivityId,
            ActivityType = request.ActivityType,
            TargetValue = request.TargetValue,
            SpeechLevel = request.SpeechLevel,
            Age = request.Age,
            Language = request.Language,
            Attempts = request.Attempts,
            ChildId = request.ChildId,
            StartedAt = request.StartedAt,
            CompletedAt = request.CompletedAt,
            MaximumAttempts = request.MaximumAttempts,
            SessionPlanSourceIds = request.SessionPlanSourceIds,
            AdditionalContext = request.AdditionalContext
        };

        return _aiCoreClient.CreateSessionSummaryAsync(aiRequest, cancellationToken);
    }
}

public sealed class CreateSessionSummaryHttpRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("activityId")]
    public string ActivityId { get; init; } = string.Empty;

    [JsonPropertyName("activityType")]
    public string ActivityType { get; init; } = string.Empty;

    [JsonPropertyName("targetValue")]
    public string TargetValue { get; init; } = string.Empty;

    [JsonPropertyName("speechLevel")]
    public string SpeechLevel { get; init; } = string.Empty;

    [JsonPropertyName("age")]
    public int Age { get; init; }

    [JsonPropertyName("language")]
    public string Language { get; init; } = "ar";

    [JsonPropertyName("attempts")]
    public List<AiSummaryAttemptInput> Attempts { get; init; } = [];

    [JsonPropertyName("childId")]
    public string? ChildId { get; init; }

    [JsonPropertyName("startedAt")]
    public DateTimeOffset? StartedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    [JsonPropertyName("maximumAttempts")]
    public int? MaximumAttempts { get; init; }

    [JsonPropertyName("sessionPlanSourceIds")]
    public List<Guid>? SessionPlanSourceIds { get; init; }

    [JsonPropertyName("additionalContext")]
    public string? AdditionalContext { get; init; }
}

public static class CreateSessionSummaryEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/summary", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor", "Parent"))
            .WithName("CreateSessionSummaryAi")
            .WithSummary("Generate an AI session summary via AI Core")
            .Accepts<CreateSessionSummaryHttpRequest>("application/json")
            .Produces<AiSessionSummaryResponse>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status422UnprocessableEntity)
            .Produces<Error>(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleAsync(
        CreateSessionSummaryHttpRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateSessionSummaryCommand(
            request.SessionId,
            request.ActivityId,
            request.ActivityType,
            request.TargetValue,
            request.SpeechLevel,
            request.Age,
            request.Language,
            request.Attempts,
            request.ChildId,
            request.StartedAt,
            request.CompletedAt,
            request.MaximumAttempts,
            request.SessionPlanSourceIds,
            request.AdditionalContext);

        Result<AiSessionSummaryResponse> result =
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
