using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Sessions.Summary.GetChildSessionHistory;

public sealed record ChildSessionHistoryItemDto
{
    public int SessionId { get; init; }
    public int ChildId { get; init; }
    public string SessionTitle { get; init; } = string.Empty;
    public string? Prompt { get; init; }
    public string? ActivityType { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public bool HasSummary { get; init; }
    public string? Outcome { get; init; }
    public string? OutcomeLabel { get; init; }
    public string? ShortSummary { get; init; }
    public int? TotalAttempts { get; init; }
    public int? SuccessfulAttempts { get; init; }
    public decimal? AverageScore { get; init; }
}

public sealed record GetChildSessionHistoryQuery(int ChildId, string UserId)
    : IRequest<Result<IReadOnlyList<ChildSessionHistoryItemDto>>>;

public sealed class GetChildSessionHistoryQueryValidator : AbstractValidator<GetChildSessionHistoryQuery>
{
    public GetChildSessionHistoryQueryValidator()
    {
        RuleFor(x => x.ChildId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class GetChildSessionHistoryQueryHandler
    : IRequestHandler<GetChildSessionHistoryQuery, Result<IReadOnlyList<ChildSessionHistoryItemDto>>>
{
    private readonly AppDbContext _db;

    public GetChildSessionHistoryQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<ChildSessionHistoryItemDto>>> Handle(
        GetChildSessionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        ChildOwnershipStatus ownership = await SessionOwnership.CheckChildOwnershipAsync(
            _db, request.UserId, request.ChildId, cancellationToken);

        if (ownership == ChildOwnershipStatus.ChildNotFound)
        {
            return Result.Failure<IReadOnlyList<ChildSessionHistoryItemDto>>(SessionSummaryErrors.ChildNotFound);
        }

        if (ownership == ChildOwnershipStatus.Forbidden)
        {
            return Result.Failure<IReadOnlyList<ChildSessionHistoryItemDto>>(SessionSummaryErrors.Forbidden);
        }

        var rows = await _db.Sessions
            .AsNoTracking()
            .Where(session =>
                session.ChildId == request.ChildId
                && !session.IsDeleted
                && session.Status == SessionStatus.Completed)
            .GroupJoin(
                _db.SessionSummaries.AsNoTracking().Where(s => !s.IsDeleted),
                session => session.Id,
                summary => summary.SessionId,
                (session, summaries) => new { session, summaries })
            .SelectMany(
                x => x.summaries.DefaultIfEmpty(),
                (x, summary) => new
                {
                    x.session.Id,
                    x.session.ChildId,
                    x.session.SessionTitle,
                    x.session.Prompt,
                    x.session.ActivityType,
                    x.session.StartedAt,
                    x.session.EndedAt,
                    HasSummary = summary != null,
                    Outcome = summary != null ? summary.Outcome : null,
                    ShortSummary = summary != null ? summary.AISummary : null,
                    TotalAttempts = summary != null ? (int?)summary.TotalAttempts : null,
                    SuccessfulAttempts = summary != null ? (int?)summary.SuccessfulAttempts : null,
                    AverageScore = summary != null ? (decimal?)summary.AverageScore : null
                })
            .OrderByDescending(row => row.EndedAt ?? row.StartedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        IReadOnlyList<ChildSessionHistoryItemDto> items = rows
            .Select(row => new ChildSessionHistoryItemDto
            {
                SessionId = row.Id,
                ChildId = row.ChildId,
                SessionTitle = row.SessionTitle,
                Prompt = row.Prompt,
                ActivityType = row.ActivityType,
                StartedAt = row.StartedAt,
                EndedAt = row.EndedAt,
                HasSummary = row.HasSummary,
                Outcome = row.Outcome,
                OutcomeLabel = row.Outcome is null
                    ? null
                    : SessionSummaryAnalytics.MapDoctorOutcomeLabel(row.Outcome),
                ShortSummary = row.ShortSummary,
                TotalAttempts = row.TotalAttempts,
                SuccessfulAttempts = row.SuccessfulAttempts,
                AverageScore = row.AverageScore
            })
            .ToArray();

        return Result.Success(items);
    }
}

public sealed class GetChildSessionHistoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/children/{childId:int}/sessions/history", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor", "Parent"))
            .WithName("GetChildSessionHistory")
            .WithTags("Sessions")
            .WithSummary("List completed therapy sessions for a child (history with summary status)")
            .Produces<IReadOnlyList<ChildSessionHistoryItemDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        int childId,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Result<IReadOnlyList<ChildSessionHistoryItemDto>> result = await sender.Send(
            new GetChildSessionHistoryQuery(childId, userId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
