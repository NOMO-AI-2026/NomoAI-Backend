using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime;
using NomoAI.API.Persistence;
using System.Security.Claims;

namespace NomoAI.API.Features.Sessions.Runtime.StartSession;

public sealed record StartSessionCommand(
    int ChildId,
    int ActivityId,
    int? DurationMinutes,
    int? MaxSteps,
    string? Language,
    string UserId) : IRequest<Result<SessionRuntimeResponse>>;

public sealed class StartSessionCommandValidator : AbstractValidator<StartSessionCommand>
{
    public StartSessionCommandValidator()
    {
        RuleFor(x => x.ChildId).GreaterThan(0);
        RuleFor(x => x.ActivityId).GreaterThan(0);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 45).When(x => x.DurationMinutes is not null);
        RuleFor(x => x.MaxSteps).InclusiveBetween(2, 12).When(x => x.MaxSteps is not null);
        RuleFor(x => x.Language).MaximumLength(16).When(x => x.Language is not null);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class StartSessionCommandHandler
    : IRequestHandler<StartSessionCommand, Result<SessionRuntimeResponse>>
{
    private readonly AppDbContext _db;
    private readonly IAiCoreClient _aiCoreClient;

    public StartSessionCommandHandler(AppDbContext db, IAiCoreClient aiCoreClient)
    {
        _db = db;
        _aiCoreClient = aiCoreClient;
    }

    public async Task<Result<SessionRuntimeResponse>> Handle(
        StartSessionCommand request,
        CancellationToken cancellationToken)
    {
        ChildOwnershipStatus ownership = await SessionOwnership.CheckChildOwnershipAsync(
            _db, request.UserId, request.ChildId, cancellationToken);

        if (ownership == ChildOwnershipStatus.ChildNotFound)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.ChildNotFound);
        }

        if (ownership == ChildOwnershipStatus.Forbidden)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.Forbidden);
        }

        NomoAI.API.Domain.Entities.Children? child = await _db.Children
            .Include(c => c.SpeechLevel)
            .FirstOrDefaultAsync(c => c.Id == request.ChildId && !c.IsDeleted, cancellationToken);

        if (child is null)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.ChildNotFound);
        }

        if (child.SpeechLevel is null)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.SpeechLevelNotFound);
        }

        Activity? activity = await _db.Activities
            .FirstOrDefaultAsync(
                a => a.Id == request.ActivityId && !a.IsDeleted,
                cancellationToken);

        if (activity is null)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.ActivityNotFound);
        }

        if (activity.ChildId != request.ChildId)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.ActivityDoesNotBelongToChild);
        }

        if (!ActivitySessionGate.IsAvailableForNewSession(activity))
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.ActivitySessionAlreadyCreated);
        }

        string activityType = MapActivityTargetType(activity.ActivityTarget);
        string prompt = activity.Content;
        int age = Math.Clamp(child.Age, 2, 18);
        string speechLevel = child.SpeechLevel.LevelName;
        string language = string.IsNullOrWhiteSpace(request.Language) ? "ar" : request.Language;

        var planRequest = new AiSessionPlanV2Request
        {
            ActivityType = activityType,
            Prompt = prompt,
            SpeechLevel = speechLevel,
            Age = age,
            Language = language,
            DurationMinutes = request.DurationMinutes ?? 15,
            MaxSteps = request.MaxSteps ?? 8
        };

        Result<AiSessionPlanV2Response> planResult =
            await _aiCoreClient.PlanSessionAsync(planRequest, cancellationToken);

        if (planResult.IsFailure)
        {
            return Result.Failure<SessionRuntimeResponse>(planResult.Error);
        }

        // Re-check after slow AI planning so a just-completed session cannot race a start.
        await _db.Entry(activity).ReloadAsync(cancellationToken);
        if (!ActivitySessionGate.IsAvailableForNewSession(activity))
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.ActivitySessionAlreadyCreated);
        }

        AiSessionPlanV2Response plan = planResult.Value;
        DateTime now = DateTime.UtcNow;
        AiSessionStepDto? firstStep = SessionPlanSnapshot.GetStep(plan, 1);
        string firstSpoken = firstStep?.Avatar?.SpokenText ?? string.Empty;

        // Synthesize step-1 speech while we persist the session row.
        Task<(string? Base64, string? ContentType)> speechTask =
            SessionSpeechEmbedder.TryBufferAsync(
                _aiCoreClient,
                firstSpoken,
                purpose: "instruction",
                cancellationToken);

        var session = new Session
        {
            ChildId = request.ChildId,
            ActivityId = request.ActivityId,
            SessionTitle = string.IsNullOrWhiteSpace(plan.Title) ? "Therapy session" : plan.Title,
            Status = SessionStatus.InProgress,
            StartedAt = now,
            PlanJson = SessionPlanSnapshot.Serialize(plan),
            CurrentStepNumber = 1,
            CurrentAttemptNumber = 0,
            PlanModel = plan.Model,
            PlanUsedFallback = plan.UsedFallback,
            PlanGeneratedAt = plan.GeneratedAt,
            KnowledgeSourceIdsJson = System.Text.Json.JsonSerializer.Serialize(plan.KnowledgeSourceIds),
            KnowledgeChunkIdsJson = System.Text.Json.JsonSerializer.Serialize(plan.KnowledgeChunkIds),
            RequiresDoctorReview = false,
            ActivityType = activityType,
            Prompt = prompt,
            SpeechLevel = speechLevel,
            Language = language,
            ChildAge = age
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        (string? speechBase64, string? speechContentType) = await speechTask;

        var response = new SessionRuntimeResponse
        {
            SessionId = session.Id,
            Status = SessionRuntimeStatus.InProgress,
            CurrentStep = firstStep is null ? null : MapStep(firstStep, attemptNumber: 0),
            Command = SessionRuntimeCommand.PlayAvatarSpeech,
            ActivityType = activityType,
            Prompt = prompt,
            SpeechAudioBase64 = speechBase64,
            SpeechAudioContentType = speechContentType
        };

        return Result.Success(response);
    }

    private static string MapActivityTargetType(ActivityTargetType target) => target switch
    {
        ActivityTargetType.OneCharacter => "character",
        ActivityTargetType.OneWord => "word",
        ActivityTargetType.Sentence => "sentence",
        _ => "word"
    };

    internal static SessionRuntimeStepDto MapStep(AiSessionStepDto step, int attemptNumber)
    {
        bool planExpectsResponse = SessionStepTypes.ExpectsChildResponse(step.Type, step.ExpectedChildAction);
        int maximumAttempts = SessionStepTypes.EffectiveMaximumAttempts(
            step.Type, step.ExpectedChildAction, step.MaximumAttempts);

        return new SessionRuntimeStepDto
        {
            StepNumber = step.StepNumber,
            StepType = step.Type,
            SpokenText = step.Avatar?.SpokenText ?? string.Empty,
            // Do not invite another recording once the step attempt budget is spent.
            ExpectsChildResponse = planExpectsResponse && attemptNumber < maximumAttempts,
            MaximumAttempts = maximumAttempts,
            AttemptNumber = attemptNumber,
            AvatarEmotion = step.Avatar?.Emotion
        };
    }
}

public sealed record StartSessionRequest(
    int ChildId,
    int ActivityId,
    int? DurationMinutes,
    int? MaxSteps,
    string? Language);

public static class StartSessionEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor", "Parent"))
            .WithName("StartSession")
            .WithSummary("Start a therapy session runtime for a child + activity")
            .Produces<SessionRuntimeResponse>(StatusCodes.Status201Created)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        StartSessionRequest request,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var command = new StartSessionCommand(
            request.ChildId,
            request.ActivityId,
            request.DurationMinutes,
            request.MaxSteps,
            request.Language,
            userId);

        Result<SessionRuntimeResponse> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(result.Error, statusCode: result.Error.StatusCode);
        }

        return Results.Created($"/api/sessions/{result.Value.SessionId}/runtime", result.Value);
    }
}
