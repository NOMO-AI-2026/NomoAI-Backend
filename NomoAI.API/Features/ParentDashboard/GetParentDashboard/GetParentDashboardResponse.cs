namespace NomoAI.API.Features.ParentDashboard.GetParentDashboard;

public sealed record GetParentDashboardResponse(
    DateTime GeneratedAtUtc,
    IReadOnlyList<ParentDashboardChildCardResponse> Children);

public sealed record ParentDashboardChildCardResponse(
    int ChildId,
    string FullName,
    ParentDashboardProgressResponse Progress,
    ParentDashboardActivityResponse Activity,
    ParentDashboardDoctorNoteResponse? LatestDoctorNote);

public sealed record ParentDashboardProgressResponse(
    int CompletedSpeechLevels,
    int TotalSpeechLevels,
    string? CurrentGoal);

public sealed record ParentDashboardActivityResponse(
    int PendingExercisesCount,
    int SessionsCompletedLast7Days);

public sealed record ParentDashboardDoctorNoteResponse(
    string NoteTitle,
    string NoteContent,
    string DoctorFullName,
    DateTime CreatedAt);
