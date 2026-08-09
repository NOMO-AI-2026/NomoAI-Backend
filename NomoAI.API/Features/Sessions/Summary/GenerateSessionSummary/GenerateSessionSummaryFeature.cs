using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary.GenerateSessionSummary;

public sealed record GenerateSessionSummaryCommand(int SessionId, string UserId, bool ForceRegenerate = false)
    : IRequest<Result<SessionSummaryDto>>;

public sealed class GenerateSessionSummaryCommandValidator : AbstractValidator<GenerateSessionSummaryCommand>
{
    public GenerateSessionSummaryCommandValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class GenerateSessionSummaryCommandHandler
    : IRequestHandler<GenerateSessionSummaryCommand, Result<SessionSummaryDto>>
{
    private readonly AppDbContext _db;

    public GenerateSessionSummaryCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SessionSummaryDto>> Handle(
        GenerateSessionSummaryCommand request,
        CancellationToken cancellationToken)
    {
        (ChildOwnershipStatus status, Session? session) =
            await SessionOwnership.CheckSessionOwnershipAsync(
                _db, request.UserId, request.SessionId, cancellationToken);

        if (status == ChildOwnershipStatus.ChildNotFound || session is null)
        {
            return Result.Failure<SessionSummaryDto>(SessionSummaryErrors.SessionNotFound);
        }

        if (status == ChildOwnershipStatus.Forbidden)
        {
            return Result.Failure<SessionSummaryDto>(SessionSummaryErrors.Forbidden);
        }

        if (session.Status != SessionStatus.Completed)
        {
            return Result.Failure<SessionSummaryDto>(SessionSummaryErrors.SessionNotCompleted);
        }

        string childName = await _db.Children
            .AsNoTracking()
            .Where(c => c.Id == session.ChildId && !c.IsDeleted)
            .Select(c => c.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "الطفل";

        SessionSummary? existing = await _db.SessionSummaries
            .FirstOrDefaultAsync(
                s => s.SessionId == session.Id && !s.IsDeleted,
                cancellationToken);

        IReadOnlyList<SessionSummaryAnalytics.AttemptSignal> signals =
            await SessionSummaryAnalytics.LoadAttemptSignalsAsync(_db, session.Id, cancellationToken);

        if (signals.Count == 0)
        {
            return Result.Failure<SessionSummaryDto>(SessionSummaryErrors.NoAttempts);
        }

        // Always refresh analytics from DB so attempt counts / scores stay truthful.
        // force=false still updates when an older soft-AI summary exists.
        bool shouldRewrite = existing is null
            || request.ForceRegenerate
            || existing.SummaryGenerationMode != "db_analytics"
            || existing.TotalAttempts != signals.Count;

        if (existing is not null && !shouldRewrite)
        {
            return Result.Success(SessionSummaryMapper.ToDto(session, existing, childName));
        }

        SessionSummaryAnalytics.ComputedAnalytics analytics =
            SessionSummaryAnalytics.Compute(session, signals);

        SessionSummary entity = existing ?? new SessionSummary
        {
            SessionId = session.Id,
            CreatedAt = DateTime.UtcNow
        };

        if (existing is null)
        {
            _db.SessionSummaries.Add(entity);
        }

        SessionSummaryAnalytics.ApplyToEntity(entity, analytics);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(SessionSummaryMapper.ToDto(session, entity, childName));
    }
}

public static class GenerateSessionSummaryEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/{sessionId:int}/summary", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor", "Parent"))
            .WithName("GenerateSessionSummary")
            .WithSummary("Generate and persist an analytical session summary from recorded attempts")
            .Produces<SessionSummaryDto>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound)
            .Produces<Error>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        int sessionId,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken,
        bool force = false)
    {
        string? userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Result<SessionSummaryDto> result = await sender.Send(
            new GenerateSessionSummaryCommand(sessionId, userId, force),
            cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(result.Error, statusCode: result.Error.StatusCode);
        }

        return Results.Ok(result.Value);
    }
}
