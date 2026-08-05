using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Dashboard;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.DoctorDashboard.GetDoctorDashboard;

internal sealed class GetDoctorDashboardHandler
    : IRequestHandler<
        GetDoctorDashboardQuery,
        Result<GetDoctorDashboardResponse>>
{
    private readonly AppDbContext _dbContext;

    public GetDoctorDashboardHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetDoctorDashboardResponse>> Handle(
        GetDoctorDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await _dbContext.Doctor
            .AsNoTracking()
            .Where(d =>
                d.UserId == request.DoctorUserId &&
                !d.IsDeleted)
            .Select(d => new
            {
                d.Id,
                d.IsApproved
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (doctor is null)
        {
            return Result.Failure<GetDoctorDashboardResponse>(
                DoctorDashboardErrors.DoctorNotFound);
        }

        if (!doctor.IsApproved)
        {
            return Result.Failure<GetDoctorDashboardResponse>(
                DoctorDashboardErrors.DoctorNotApproved);
        }

        DateTime utcNow = DateTime.UtcNow;
        DateTime startOfTodayUtc =
            DashboardRules.GetUtcStartOfToday(utcNow);
        DateTime startOfTomorrowUtc =
            DashboardRules.GetUtcStartOfTomorrow(utcNow);
        DateTime recentWindowStartUtc =
            DashboardRules.GetRecentWindowStartUtc(utcNow);

        var sessionStats = await _dbContext.Sessions
            .AsNoTracking()
            .Where(session =>
                !session.IsDeleted &&
                !session.Child.IsDeleted &&
                session.Child.DoctorId == doctor.Id)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                CompletedToday = group.Count(session =>
                    session.Status == SessionStatus.Completed &&
                    (session.EndedAt ?? session.StartedAt) >=
                        startOfTodayUtc &&
                    (session.EndedAt ?? session.StartedAt) <
                        startOfTomorrowUtc),
                CompletedLast7Days = group.Count(session =>
                    session.Status == SessionStatus.Completed &&
                    (session.EndedAt ?? session.StartedAt) >=
                        recentWindowStartUtc &&
                    (session.EndedAt ?? session.StartedAt) <=
                        utcNow),
                AwaitingDoctorReview = group.Count(session =>
                    session.Status == SessionStatus.Completed &&
                    !session.IsDoctorReviewed)
            })
            .FirstOrDefaultAsync(cancellationToken);

        int completedToday = sessionStats?.CompletedToday ?? 0;
        int completedLast7Days =
            sessionStats?.CompletedLast7Days ?? 0;
        int awaitingDoctorReview =
            sessionStats?.AwaitingDoctorReview ?? 0;

        var children = await _dbContext.Children
            .AsNoTracking()
            .Where(child =>
                child.DoctorId == doctor.Id &&
                !child.IsDeleted)
            .Select(child => new
            {
                child.Id,
                child.FullName
            })
            .ToListAsync(cancellationToken);

        int totalChildren = children.Count;

        // Assumption (v1): SpeechLevel.Id order represents level rank
        // (see DashboardRules.ClassifyLevelChange).
        var historiesInWindow = await _dbContext
            .ChildSpeechLevelHistories
            .AsNoTracking()
            .Where(history =>
                !history.IsDeleted &&
                !history.Child.IsDeleted &&
                history.Child.DoctorId == doctor.Id &&
                history.ChangedAt >= recentWindowStartUtc &&
                history.ChangedAt <= utcNow)
            .Select(history => new
            {
                history.Id,
                history.ChildId,
                history.PreviousSpeechLevelId,
                history.NewSpeechLevelId,
                history.ChangedAt
            })
            .ToListAsync(cancellationToken);

        var latestHistoryByChild = historiesInWindow
            .GroupBy(history => history.ChildId)
            .Select(group => group
                .OrderByDescending(history => history.ChangedAt)
                .ThenByDescending(history => history.Id)
                .First())
            .ToList();

        var progressed = new List<DoctorDashboardChildItemResponse>();
        var regressed = new List<DoctorDashboardChildItemResponse>();
        var childrenWithProgressOrRegress = new HashSet<int>();

        var childLookup = children.ToDictionary(
            child => child.Id,
            child => child.FullName);

        foreach (var history in latestHistoryByChild)
        {
            if (!childLookup.TryGetValue(
                    history.ChildId,
                    out string? fullName))
            {
                continue;
            }

            // ClassifyLevelChange uses SpeechLevel.Id order as rank (v1).
            SpeechLevelChangeKind changeKind =
                DashboardRules.ClassifyLevelChange(
                    history.PreviousSpeechLevelId,
                    history.NewSpeechLevelId);

            if (changeKind == SpeechLevelChangeKind.Progressed)
            {
                progressed.Add(
                    new DoctorDashboardChildItemResponse(
                        history.ChildId,
                        fullName));
                childrenWithProgressOrRegress.Add(history.ChildId);
            }
            else if (changeKind == SpeechLevelChangeKind.Regressed)
            {
                regressed.Add(
                    new DoctorDashboardChildItemResponse(
                        history.ChildId,
                        fullName));
                childrenWithProgressOrRegress.Add(history.ChildId);
            }
            // Same-level history (Stable) is treated like "no meaningful
            // change" and lands in stableLast7Days with children who had
            // no history rows in the window.
        }

        // Stable = no speech-level history change in the last 7 days
        // (or latest window row classified as Stable / same level).
        var stable = children
            .Where(child =>
                !childrenWithProgressOrRegress.Contains(child.Id))
            .Select(child => new DoctorDashboardChildItemResponse(
                child.Id,
                child.FullName))
            .OrderBy(item => item.FullName)
            .ThenBy(item => item.ChildId)
            .ToList();

        List<DoctorDashboardChildItemResponse> progressedSorted =
            progressed
                .OrderBy(item => item.FullName)
                .ThenBy(item => item.ChildId)
                .ToList();

        List<DoctorDashboardChildItemResponse> regressedSorted =
            regressed
                .OrderBy(item => item.FullName)
                .ThenBy(item => item.ChildId)
                .ToList();

        var response = new GetDoctorDashboardResponse(
            GeneratedAtUtc: utcNow,
            Sessions: new DoctorDashboardSessionsResponse(
                CompletedToday: completedToday,
                CompletedLast7Days: completedLast7Days,
                AwaitingDoctorReview: awaitingDoctorReview),
            Cases: new DoctorDashboardCasesResponse(
                TotalChildren: totalChildren,
                ProgressedLast7Days: progressedSorted,
                StableLast7Days: stable,
                RegressedLast7Days: regressedSorted));

        return Result.Success(response);
    }
}
