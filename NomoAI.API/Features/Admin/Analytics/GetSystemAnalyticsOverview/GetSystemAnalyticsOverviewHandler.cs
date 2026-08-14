using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Admin.Analytics.GetSystemAnalyticsOverview;

internal sealed class GetSystemAnalyticsOverviewHandler
    : IRequestHandler<
        GetSystemAnalyticsOverviewQuery,
        Result<GetSystemAnalyticsOverviewResponse>>
{
    private readonly AppDbContext _dbContext;

    public GetSystemAnalyticsOverviewHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetSystemAnalyticsOverviewResponse>> Handle(
        GetSystemAnalyticsOverviewQuery request,
        CancellationToken cancellationToken)
    {
        int parentsTotal = await _dbContext.Parents
            .AsNoTracking()
            .CountAsync(
                parent => !parent.IsDeleted && !parent.User.IsDeleted,
                cancellationToken);

        var doctorApprovalCounts = await _dbContext.Doctor
            .AsNoTracking()
            .Where(doctor => !doctor.IsDeleted && !doctor.User.IsDeleted)
            .GroupBy(doctor => doctor.IsApproved)
            .Select(group => new
            {
                IsApproved = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        int doctorsApproved = doctorApprovalCounts
            .Where(item => item.IsApproved)
            .Select(item => item.Count)
            .FirstOrDefault();

        int doctorsPendingApproval = doctorApprovalCounts
            .Where(item => !item.IsApproved)
            .Select(item => item.Count)
            .FirstOrDefault();

        int doctorsTotal = doctorsApproved + doctorsPendingApproval;

        var childrenStats = await _dbContext.Children
            .AsNoTracking()
            .Where(child => !child.IsDeleted)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                WithActiveParent = group.Count(child =>
                    child.ParentId != null &&
                    child.Parent != null &&
                    !child.Parent.IsDeleted &&
                    !child.Parent.User.IsDeleted)
            })
            .FirstOrDefaultAsync(cancellationToken);

        int childrenTotal = childrenStats?.Total ?? 0;
        int childrenWithParent = childrenStats?.WithActiveParent ?? 0;
        int childrenWithoutParent = childrenTotal - childrenWithParent;

        int activitiesTotal = await _dbContext.Activities
            .AsNoTracking()
            .CountAsync(activity => !activity.IsDeleted, cancellationToken);

        // Scheduled = activities ready for a session, with no active InProgress session.
        int sessionsScheduled = await _dbContext.Activities
            .AsNoTracking()
            .CountAsync(
                activity =>
                    !activity.IsDeleted &&
                    activity.CanMakeSession &&
                    !activity.Sessions.Any(session =>
                        !session.IsDeleted &&
                        session.Status == SessionStatus.InProgress),
                cancellationToken);

        int sessionsInProgress = await _dbContext.Sessions
            .AsNoTracking()
            .CountAsync(
                session =>
                    !session.IsDeleted &&
                    session.Status == SessionStatus.InProgress,
                cancellationToken);

        int sessionsCompleted = await _dbContext.Sessions
            .AsNoTracking()
            .CountAsync(
                session =>
                    !session.IsDeleted &&
                    session.Status == SessionStatus.Completed,
                cancellationToken);

        int sessionsTotal =
            sessionsScheduled + sessionsInProgress + sessionsCompleted;

        int sessionAttemptsTotal = await _dbContext.SessionAttempts
            .AsNoTracking()
            .CountAsync(attempt => !attempt.IsDeleted, cancellationToken);

        int sessionSummariesTotal = await _dbContext.SessionSummaries
            .AsNoTracking()
            .CountAsync(summary => !summary.IsDeleted, cancellationToken);

        int attemptEvaluationsTotal = await _dbContext.AttemptEvaluations
            .AsNoTracking()
            .CountAsync(
                evaluation => !evaluation.IsDeleted,
                cancellationToken);

        int attemptTranscriptionsTotal =
            await _dbContext.AttemptTranscribtions
                .AsNoTracking()
                .CountAsync(
                    transcription => !transcription.IsDeleted,
                    cancellationToken);

        Dictionary<AlertType, int> alertTypeCounts =
            await _dbContext.ChildProgressAlerts
                .AsNoTracking()
                .Where(alert => !alert.IsDeleted)
                .GroupBy(alert => alert.alertType)
                .Select(group => new
                {
                    AlertType = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(
                    item => item.AlertType,
                    item => item.Count,
                    cancellationToken);

        int progressAlertsTotal = alertTypeCounts.Values.Sum();

        var supportStats = await _dbContext.SupportTickets
            .AsNoTracking()
            .Where(ticket => !ticket.IsDeleted)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Open = group.Count(ticket =>
                    ticket.Status == SupportTicketStatus.Open),
                InProgress = group.Count(ticket =>
                    ticket.Status == SupportTicketStatus.InProgress),
                Resolved = group.Count(ticket =>
                    ticket.Status == SupportTicketStatus.Resolved),
                Closed = group.Count(ticket =>
                    ticket.Status == SupportTicketStatus.Closed),
                AwaitingAdminAction = group.Count(ticket =>
                    ticket.Status == SupportTicketStatus.Open &&
                    ticket.HandledAt == null),
                HandledByAdmin = group.Count(ticket =>
                    ticket.HandledAt != null ||
                    ticket.Status != SupportTicketStatus.Open),
                UserMutableOpen = group.Count(ticket =>
                    ticket.Status == SupportTicketStatus.Open &&
                    ticket.HandledAt == null &&
                    ticket.HandledByAdminUserId == null),
                LockedForUser = group.Count(ticket =>
                    !(ticket.Status == SupportTicketStatus.Open &&
                      ticket.HandledAt == null &&
                      ticket.HandledByAdminUserId == null))
            })
            .FirstOrDefaultAsync(cancellationToken);

        int supportTicketsTotal = supportStats?.Total ?? 0;

        List<SpeechLevelChildrenCountResponse> childrenPerLevel =
            await _dbContext.SpeechLevels
                .AsNoTracking()
                .Where(level => !level.IsDeleted)
                .OrderBy(level => level.Id)
                .Select(level => new SpeechLevelChildrenCountResponse(
                    level.Id,
                    level.LevelName,
                    _dbContext.Children.Count(child =>
                        !child.IsDeleted &&
                        child.SpeechLevelId == level.Id)))
                .ToListAsync(cancellationToken);

        var response = new GetSystemAnalyticsOverviewResponse(
            GeneratedAtUtc: DateTime.UtcNow,
            Users: new UsersAnalyticsResponse(
                ParentsTotal: parentsTotal,
                DoctorsTotal: doctorsTotal,
                DoctorsApproved: doctorsApproved,
                DoctorsPendingApproval: doctorsPendingApproval),
            Children: new ChildrenAnalyticsResponse(
                Total: childrenTotal,
                WithParentAssigned: childrenWithParent,
                WithoutParentAssigned: childrenWithoutParent),
            Therapy: new TherapyAnalyticsResponse(
                ActivitiesTotal: activitiesTotal,
                SessionsTotal: sessionsTotal,
                SessionsByStatus: new SessionsByStatusResponse(
                    Scheduled: sessionsScheduled,
                    InProgress: sessionsInProgress,
                    Completed: sessionsCompleted),
                SessionAttemptsTotal: sessionAttemptsTotal,
                SessionSummariesTotal: sessionSummariesTotal,
                AttemptEvaluationsTotal: attemptEvaluationsTotal,
                AttemptTranscriptionsTotal: attemptTranscriptionsTotal),
            Alerts: new AlertsAnalyticsResponse(
                ProgressAlertsTotal: progressAlertsTotal,
                ByType: new AlertsByTypeResponse(
                    Milestone: alertTypeCounts.GetValueOrDefault(
                        AlertType.Milestone),
                    Improvement: alertTypeCounts.GetValueOrDefault(
                        AlertType.Improvement),
                    Concern: alertTypeCounts.GetValueOrDefault(
                        AlertType.Concern),
                    Regression: alertTypeCounts.GetValueOrDefault(
                        AlertType.Regression))),
            Support: new SupportAnalyticsResponse(
                TicketsTotal: supportTicketsTotal,
                ByStatus: new SupportTicketsByStatusResponse(
                    Unread: supportStats?.Open ?? 0,
                    InProgress: supportStats?.InProgress ?? 0,
                    Resolved: supportStats?.Resolved ?? 0,
                    Closed: supportStats?.Closed ?? 0),
                AwaitingAdminAction:
                    supportStats?.AwaitingAdminAction ?? 0,
                HandledByAdmin:
                    supportStats?.HandledByAdmin ?? 0,
                UserMutableOpen:
                    supportStats?.UserMutableOpen ?? 0,
                LockedForUser:
                    supportStats?.LockedForUser ?? 0),
            SpeechLevels: new SpeechLevelsAnalyticsResponse(
                CatalogCount: childrenPerLevel.Count,
                ChildrenPerLevel: childrenPerLevel));

        return Result.Success(response);
    }
}
