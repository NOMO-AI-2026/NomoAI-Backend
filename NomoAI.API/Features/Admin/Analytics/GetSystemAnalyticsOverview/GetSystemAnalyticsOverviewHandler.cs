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

        Dictionary<SessionStatus, int> sessionStatusCounts =
            await _dbContext.Sessions
                .AsNoTracking()
                .Where(session => !session.IsDeleted)
                .GroupBy(session => session.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(
                    item => item.Status,
                    item => item.Count,
                    cancellationToken);

        int sessionsTotal = sessionStatusCounts.Values.Sum();

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

        Dictionary<SupportTicketStatus, int> supportStatusCounts =
            await _dbContext.SupportTickets
                .AsNoTracking()
                .Where(ticket => !ticket.IsDeleted)
                .GroupBy(ticket => ticket.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(
                    item => item.Status,
                    item => item.Count,
                    cancellationToken);

        int supportTicketsTotal = supportStatusCounts.Values.Sum();

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
                    Scheduled: sessionStatusCounts.GetValueOrDefault(
                        SessionStatus.Scheduled),
                    InProgress: sessionStatusCounts.GetValueOrDefault(
                        SessionStatus.InProgress),
                    Completed: sessionStatusCounts.GetValueOrDefault(
                        SessionStatus.Completed),
                    Cancelled: sessionStatusCounts.GetValueOrDefault(
                        SessionStatus.Cancelled),
                    Missed: sessionStatusCounts.GetValueOrDefault(
                        SessionStatus.Missed)),
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
                    Open: supportStatusCounts.GetValueOrDefault(
                        SupportTicketStatus.Open),
                    InProgress: supportStatusCounts.GetValueOrDefault(
                        SupportTicketStatus.InProgress),
                    Resolved: supportStatusCounts.GetValueOrDefault(
                        SupportTicketStatus.Resolved),
                    Closed: supportStatusCounts.GetValueOrDefault(
                        SupportTicketStatus.Closed))),
            SpeechLevels: new SpeechLevelsAnalyticsResponse(
                CatalogCount: childrenPerLevel.Count,
                ChildrenPerLevel: childrenPerLevel));

        return Result.Success(response);
    }
}
