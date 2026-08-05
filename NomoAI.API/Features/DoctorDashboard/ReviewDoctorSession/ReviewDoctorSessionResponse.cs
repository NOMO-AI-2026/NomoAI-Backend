namespace NomoAI.API.Features.DoctorDashboard.ReviewDoctorSession;

public sealed record ReviewDoctorSessionResponse(
    int SessionId,
    int ChildId,
    int Rating,
    string Comment,
    bool IsDoctorReviewed,
    DateTime ReviewedAtUtc);
