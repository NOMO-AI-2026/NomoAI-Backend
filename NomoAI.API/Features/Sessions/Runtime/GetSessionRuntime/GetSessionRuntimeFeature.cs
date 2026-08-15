using FluentValidation;
using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime.StartSession;
using NomoAI.API.Persistence;
using System.Security.Claims;

namespace NomoAI.API.Features.Sessions.Runtime.GetSessionRuntime;

public sealed record GetSessionRuntimeQuery(int SessionId, string UserId)
    : IRequest<Result<SessionRuntimeResponse>>;

public sealed class GetSessionRuntimeQueryValidator : AbstractValidator<GetSessionRuntimeQuery>
{
    public GetSessionRuntimeQueryValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class GetSessionRuntimeQueryHandler
    : IRequestHandler<GetSessionRuntimeQuery, Result<SessionRuntimeResponse>>
{
    private readonly AppDbContext _db;

    public GetSessionRuntimeQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SessionRuntimeResponse>> Handle(
        GetSessionRuntimeQuery request,
        CancellationToken cancellationToken)
    {
        (ChildOwnershipStatus status, Session? session) = await SessionOwnership.CheckSessionOwnershipAsync(
            _db, request.UserId, request.SessionId, cancellationToken);

        if (status == ChildOwnershipStatus.ChildNotFound || session is null)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.SessionNotFound);
        }

        if (status == ChildOwnershipStatus.Forbidden)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.Forbidden);
        }

        AiSessionPlanV2Response? plan = SessionPlanSnapshot.Deserialize(session.PlanJson);
        bool statusChanged = SessionLifecycle.SynchronizePersistedStatus(session, plan);

        if (statusChanged && session.Status == SessionStatus.Completed)
        {
            await SessionCompletion.FinalizeAsync(_db, session, cancellationToken);
        }

        if (statusChanged || _db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(BuildRuntimeResponse(session));
    }

    internal static SessionRuntimeResponse BuildRuntimeResponse(Session session)
    {
        string status = SessionRuntimeStatus.Map(session.Status);
        AiSessionPlanV2Response? plan = SessionPlanSnapshot.Deserialize(session.PlanJson);
        AiSessionStepDto? currentPlanStep = plan is null
            ? null
            : SessionPlanSnapshot.GetStep(plan, session.CurrentStepNumber);

        if (session.Status is SessionStatus.Completed or SessionStatus.Cancelled or SessionStatus.Missed)
        {
            return new SessionRuntimeResponse
            {
                SessionId = session.Id,
                Status = status,
                CurrentStep = currentPlanStep is null
                    ? null
                    : StartSessionCommandHandler.MapStep(currentPlanStep, session.CurrentAttemptNumber),
                Command = SessionRuntimeCommand.SessionCompleted,
                ActivityType = session.ActivityType,
                Prompt = session.Prompt
            };
        }

        // Still running — never report session_completed while InProgress/Scheduled.
        if (plan is null || currentPlanStep is null)
        {
            return new SessionRuntimeResponse
            {
                SessionId = session.Id,
                Status = SessionRuntimeStatus.InProgress,
                Command = SessionRuntimeCommand.PlayAvatarSpeech,
                ActivityType = session.ActivityType,
                Prompt = session.Prompt
            };
        }

        SessionRuntimeStepDto mappedStep = StartSessionCommandHandler.MapStep(
            currentPlanStep,
            session.CurrentAttemptNumber);

        // After a retry, refresh should resume at recording rather than replaying the step intro.
        // Never resume recording when the step attempt budget is already exhausted.
        bool canRecordMore = mappedStep.ExpectsChildResponse
            && session.CurrentAttemptNumber > 0
            && session.CurrentAttemptNumber < mappedStep.MaximumAttempts;

        return new SessionRuntimeResponse
        {
            SessionId = session.Id,
            Status = SessionRuntimeStatus.InProgress,
            CurrentStep = mappedStep,
            Command = canRecordMore
                ? SessionRuntimeCommand.ReadyToRecord
                : SessionRuntimeCommand.PlayAvatarSpeech,
            ActivityType = session.ActivityType,
            Prompt = session.Prompt
        };
    }
}

public static class GetSessionRuntimeEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapGet("/{sessionId:int}/runtime", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor", "Parent"))
            .WithName("GetSessionRuntime")
            .WithSummary("Get the current runtime state of a therapy session")
            .Produces<SessionRuntimeResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        int sessionId,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Result<SessionRuntimeResponse> result =
            await sender.Send(new GetSessionRuntimeQuery(sessionId, userId), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(result.Error, statusCode: result.Error.StatusCode);
        }

        return Results.Ok(result.Value);
    }
}
