namespace NomoAI.API.Features.DoctorDashboard.GetDoctorDashboard;

public sealed record GetDoctorDashboardResponse(
    DateTime GeneratedAtUtc,
    DoctorDashboardSessionsResponse Sessions,
    DoctorDashboardCasesResponse Cases);

public sealed record DoctorDashboardSessionsResponse(
    int CompletedToday,
    int CompletedLast7Days,
    int AwaitingDoctorReview);

public sealed record DoctorDashboardCasesResponse(
    int TotalChildren,
    IReadOnlyList<DoctorDashboardChildItemResponse> ProgressedLast7Days,
    IReadOnlyList<DoctorDashboardChildItemResponse> StableLast7Days,
    IReadOnlyList<DoctorDashboardChildItemResponse> RegressedLast7Days);

public sealed record DoctorDashboardChildItemResponse(
    int ChildId,
    string FullName);
