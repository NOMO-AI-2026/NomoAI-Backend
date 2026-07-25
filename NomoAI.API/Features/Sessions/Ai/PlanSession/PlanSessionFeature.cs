using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;

namespace NomoAI.API.Features.Sessions.Ai.PlanSession;

public sealed record PlanSessionCommand(
    string? SessionId,
    string ChildId,
    string ActivityId,
    string ActivityType,
    string TargetValue,
    string SpeechLevel,
    int Age,
    string Language,
    int MaximumDurationMinutes,
    int MaximumSteps,
    string? PreviousSessionSummary,
    string? AdditionalContext) : IRequest<Result<AiSessionPlanResponse>>;

public sealed class PlanSessionCommandValidator : AbstractValidator<PlanSessionCommand>
{
    public PlanSessionCommandValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ActivityId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ActivityType).NotEmpty().MaximumLength(32);
        RuleFor(x => x.TargetValue).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SpeechLevel).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Age).InclusiveBetween(2, 18);
        RuleFor(x => x.Language).NotEmpty().MaximumLength(16);
        RuleFor(x => x.MaximumDurationMinutes).InclusiveBetween(5, 45);
        RuleFor(x => x.MaximumSteps).InclusiveBetween(2, 12);
        RuleFor(x => x.PreviousSessionSummary).MaximumLength(2000).When(x => x.PreviousSessionSummary is not null);
        RuleFor(x => x.AdditionalContext).MaximumLength(2000).When(x => x.AdditionalContext is not null);
    }
}

public sealed class PlanSessionCommandHandler
    : IRequestHandler<PlanSessionCommand, Result<AiSessionPlanResponse>>
{
    private readonly IAiCoreClient _aiCoreClient;

    public PlanSessionCommandHandler(IAiCoreClient aiCoreClient)
    {
        _aiCoreClient = aiCoreClient;
    }

    public Task<Result<AiSessionPlanResponse>> Handle(
        PlanSessionCommand request,
        CancellationToken cancellationToken)
    {
        var aiRequest = new AiSessionPlanRequest
        {
            SessionId = request.SessionId,
            ChildId = request.ChildId,
            ActivityId = request.ActivityId,
            ActivityType = request.ActivityType,
            TargetValue = request.TargetValue,
            SpeechLevel = request.SpeechLevel,
            Age = request.Age,
            Language = request.Language,
            MaximumDurationMinutes = request.MaximumDurationMinutes,
            MaximumSteps = request.MaximumSteps,
            PreviousSessionSummary = request.PreviousSessionSummary,
            AdditionalContext = request.AdditionalContext
        };

        return _aiCoreClient.PlanSessionAsync(aiRequest, cancellationToken);
    }
}

public sealed class PlanSessionHttpRequest
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; init; }

    [JsonPropertyName("childId")]
    public string ChildId { get; init; } = string.Empty;

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

    [JsonPropertyName("maximumDurationMinutes")]
    public int MaximumDurationMinutes { get; init; } = 15;

    [JsonPropertyName("maximumSteps")]
    public int MaximumSteps { get; init; } = 6;

    [JsonPropertyName("previousSessionSummary")]
    public string? PreviousSessionSummary { get; init; }

    [JsonPropertyName("additionalContext")]
    public string? AdditionalContext { get; init; }
}

public static class PlanSessionEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/plan", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor", "Parent"))
            .WithName("PlanSessionAi")
            .WithSummary("Generate an AI session plan via AI Core")
            .DisableAntiforgery()
            .Accepts<PlanSessionHttpRequest>("application/json")
            .Produces<AiSessionPlanResponse>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status422UnprocessableEntity)
            .Produces<Error>(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleAsync(
        PlanSessionHttpRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new PlanSessionCommand(
            request.SessionId,
            request.ChildId,
            request.ActivityId,
            request.ActivityType,
            request.TargetValue,
            request.SpeechLevel,
            request.Age,
            request.Language,
            request.MaximumDurationMinutes,
            request.MaximumSteps,
            request.PreviousSessionSummary,
            request.AdditionalContext);

        Result<AiSessionPlanResponse> result =
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
