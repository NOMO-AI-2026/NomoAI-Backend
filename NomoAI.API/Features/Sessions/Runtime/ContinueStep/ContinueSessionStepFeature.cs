using FluentValidation;
using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime.GetSessionRuntime;
using NomoAI.API.Features.Sessions.Summary;
using NomoAI.API.Persistence;
using System.Security.Claims;

namespace NomoAI.API.Features.Sessions.Runtime.ContinueStep;

/// <summary>
/// Advances a non-listening plan step after avatar speech finishes.
/// ASP.NET owns the transition; React only requests continuation.
/// </summary>
public sealed record ContinueSessionStepCommand(int SessionId, string UserId)
    : IRequest<Result<SessionRuntimeResponse>>;

public sealed class ContinueSessionStepCommandValidator : AbstractValidator<ContinueSessionStepCommand>
{
    public ContinueSessionStepCommandValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class ContinueSessionStepCommandHandler
    : IRequestHandler<ContinueSessionStepCommand, Result<SessionRuntimeResponse>>
{
    private readonly AppDbContext _db;
    private readonly IAiCoreClient _aiCoreClient;

    public ContinueSessionStepCommandHandler(AppDbContext db, IAiCoreClient aiCoreClient)
    {
        _db = db;
        _aiCoreClient = aiCoreClient;
    }

    public async Task<Result<SessionRuntimeResponse>> Handle(
        ContinueSessionStepCommand request,
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

        if (session.Status != SessionStatus.InProgress)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.SessionNotInProgress);
        }

        AiSessionPlanV2Response? plan = SessionPlanSnapshot.Deserialize(session.PlanJson);
        if (plan is null)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.PlanUnavailable);
        }

        AiSessionStepDto? currentStep = SessionPlanSnapshot.GetStep(plan, session.CurrentStepNumber);
        if (currentStep is null)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.StepNotFound);
        }

        if (SessionStepTypes.ExpectsChildResponse(currentStep.Type, currentStep.ExpectedChildAction))
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.ChildResponseRequired);
        }

        int nextStepNumber = session.CurrentStepNumber + 1;
        session.CurrentAttemptNumber = 0;

        if (SessionPlanSnapshot.GetStep(plan, nextStepNumber) is null)
        {
            session.Status = SessionStatus.Completed;
            session.EndedAt = DateTime.UtcNow;
            await ActivitySessionGate.MarkUnavailableAfterCompletedSessionAsync(
                _db,
                session.ActivityId,
                cancellationToken);
            await SessionSummaryPersister.TryPersistForCompletedSessionAsync(
                _db,
                session,
                cancellationToken);
        }
        else
        {
            session.CurrentStepNumber = nextStepNumber;
        }

        AiSessionStepDto? nextStep = session.Status == SessionStatus.Completed
            ? null
            : SessionPlanSnapshot.GetStep(plan, session.CurrentStepNumber);
        string nextSpoken = nextStep?.Avatar?.SpokenText ?? string.Empty;

        Task<(string? Base64, string? ContentType)> speechTask =
            SessionSpeechEmbedder.TryBufferAsync(
                _aiCoreClient,
                nextSpoken,
                purpose: "instruction",
                cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        SessionRuntimeResponse response = GetSessionRuntimeQueryHandler.BuildRuntimeResponse(session);

        if (response.Command == SessionRuntimeCommand.PlayAvatarSpeech && !string.IsNullOrWhiteSpace(nextSpoken))
        {
            (string? speechBase64, string? speechContentType) = await speechTask;
            response = new SessionRuntimeResponse
            {
                SessionId = response.SessionId,
                Status = response.Status,
                CurrentStep = response.CurrentStep,
                Command = response.Command,
                Feedback = response.Feedback,
                ActivityType = response.ActivityType,
                Prompt = response.Prompt,
                SpeechAudioBase64 = speechBase64,
                SpeechAudioContentType = speechContentType
            };
        }

        return Result.Success(response);
    }
}

public static class ContinueSessionStepEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/{sessionId:int}/steps/continue", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor", "Parent"))
            .WithName("ContinueSessionStep")
            .WithSummary("Advance a non-listening step after avatar speech ends")
            .Produces<SessionRuntimeResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound)
            .Produces<Error>(StatusCodes.Status409Conflict);
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
            await sender.Send(new ContinueSessionStepCommand(sessionId, userId), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(result.Error, statusCode: result.Error.StatusCode);
        }

        return Results.Ok(result.Value);
    }
}
