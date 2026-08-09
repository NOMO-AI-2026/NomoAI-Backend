using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Sessions.Runtime;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary.GetParentSessionSummary;

public sealed record GetParentSessionSummaryQuery(int SessionId, string UserId)
    : IRequest<Result<ParentSessionSummaryResponse>>;

public sealed class GetParentSessionSummaryQueryValidator : AbstractValidator<GetParentSessionSummaryQuery>
{
    public GetParentSessionSummaryQueryValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class GetParentSessionSummaryQueryHandler
    : IRequestHandler<GetParentSessionSummaryQuery, Result<ParentSessionSummaryResponse>>
{
    private readonly AppDbContext _db;

    public GetParentSessionSummaryQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ParentSessionSummaryResponse>> Handle(
        GetParentSessionSummaryQuery request,
        CancellationToken cancellationToken)
    {
        (ChildOwnershipStatus status, Session? session) =
            await SessionOwnership.CheckSessionOwnershipAsync(
                _db, request.UserId, request.SessionId, cancellationToken);

        if (status == ChildOwnershipStatus.ChildNotFound || session is null)
        {
            return Result.Failure<ParentSessionSummaryResponse>(SessionSummaryErrors.SessionNotFound);
        }

        if (status == ChildOwnershipStatus.Forbidden)
        {
            return Result.Failure<ParentSessionSummaryResponse>(SessionSummaryErrors.Forbidden);
        }

        bool isParent = await _db.Parents
            .AsNoTracking()
            .AnyAsync(p => p.UserId == request.UserId && !p.IsDeleted, cancellationToken);

        bool isDoctor = await _db.Doctor
            .AsNoTracking()
            .AnyAsync(d => d.UserId == request.UserId && !d.IsDeleted, cancellationToken);

        if (!isParent && !isDoctor)
        {
            return Result.Failure<ParentSessionSummaryResponse>(SessionSummaryErrors.Forbidden);
        }

        SessionSummary? summary = await _db.SessionSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.SessionId == session.Id && !s.IsDeleted,
                cancellationToken);

        if (summary is null)
        {
            return Result.Failure<ParentSessionSummaryResponse>(SessionSummaryErrors.SummaryNotFound);
        }

        string childName = await _db.Children
            .AsNoTracking()
            .Where(c => c.Id == session.ChildId && !c.IsDeleted)
            .Select(c => c.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "طفلك";

        SessionSummaryDto dto = SessionSummaryMapper.ToDto(session, summary, childName);
        return Result.Success(SessionSummaryMapper.ToParentResponse(dto));
    }
}

public sealed class GetParentSessionSummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/parent/sessions/{sessionId:int}/summary", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Parent", "Doctor"))
            .WithName("GetParentSessionSummary")
            .WithTags("ParentDashboard")
            .WithSummary("Get a simplified parent-safe session summary")
            .Produces<ParentSessionSummaryResponse>(StatusCodes.Status200OK)
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

        Result<ParentSessionSummaryResponse> result = await sender.Send(
            new GetParentSessionSummaryQuery(sessionId, userId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
