using System.Text.Json.Serialization;

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
    [property: JsonPropertyName("sessionsByStatus")] SessionsByStatusResponse SessionsByStatus,
    int SessionAttemptsTotal,
    int SessionSummariesTotal,
    int AttemptEvaluationsTotal,
    int AttemptTranscriptionsTotal);

public sealed record SessionsByStatusResponse(
    [property: JsonPropertyName("scheduled")] int Scheduled,
    [property: JsonPropertyName("inProgress")] int InProgress,
    [property: JsonPropertyName("completed")] int Completed,
    [property: JsonPropertyName("cancelled")] int Cancelled,
    [property: JsonPropertyName("missed")] int Missed);

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
    int Unread,
    [property: JsonPropertyName("inProgress")] int InProgress,
    int Resolved,
    int Closed);

public sealed record SpeechLevelsAnalyticsResponse(
    int CatalogCount,
    IReadOnlyList<SpeechLevelChildrenCountResponse> ChildrenPerLevel);

public sealed record SpeechLevelChildrenCountResponse(
    int SpeechLevelId,
    string Name,
    int ChildrenCount);
