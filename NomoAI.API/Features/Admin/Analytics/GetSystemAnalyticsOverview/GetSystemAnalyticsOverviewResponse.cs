namespace NomoAI.API.Features.Admin.Analytics.GetSystemAnalyticsOverview;

public sealed record GetSystemAnalyticsOverviewResponse(
    DateTime GeneratedAtUtc,
    UsersAnalyticsResponse Users,
    ChildrenAnalyticsResponse Children,
    TherapyAnalyticsResponse Therapy,
    AlertsAnalyticsResponse Alerts,
    SupportAnalyticsResponse Support,
    SpeechLevelsAnalyticsResponse SpeechLevels);

public sealed record UsersAnalyticsResponse(
    int ParentsTotal,
    int DoctorsTotal,
    int DoctorsApproved,
    int DoctorsPendingApproval);

public sealed record ChildrenAnalyticsResponse(
    int Total,
    int WithParentAssigned,
    int WithoutParentAssigned);

public sealed record TherapyAnalyticsResponse(
    int ActivitiesTotal,
    int SessionsTotal,
    SessionsByStatusResponse SessionsByStatus,
    int SessionAttemptsTotal,
    int SessionSummariesTotal,
    int AttemptEvaluationsTotal,
    int AttemptTranscriptionsTotal);

public sealed record SessionsByStatusResponse(
    int Scheduled,
    int InProgress,
    int Completed,
    int Cancelled,
    int Missed);

public sealed record AlertsAnalyticsResponse(
    int ProgressAlertsTotal,
    AlertsByTypeResponse ByType);

public sealed record AlertsByTypeResponse(
    int Milestone,
    int Improvement,
    int Concern,
    int Regression);

public sealed record SupportAnalyticsResponse(
    int TicketsTotal,
    SupportTicketsByStatusResponse ByStatus,
    int AwaitingAdminAction,
    int HandledByAdmin,
    int UserMutableOpen,
    int LockedForUser);

public sealed record SupportTicketsByStatusResponse(
    int Open,
    int InProgress,
    int Resolved,
    int Closed);

public sealed record SpeechLevelsAnalyticsResponse(
    int CatalogCount,
    IReadOnlyList<SpeechLevelChildrenCountResponse> ChildrenPerLevel);

public sealed record SpeechLevelChildrenCountResponse(
    int SpeechLevelId,
    string Name,
    int ChildrenCount);
