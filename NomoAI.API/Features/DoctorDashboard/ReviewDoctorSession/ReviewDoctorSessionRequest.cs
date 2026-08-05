namespace NomoAI.API.Features.DoctorDashboard.ReviewDoctorSession;

public sealed record ReviewDoctorSessionRequest(
    int Rating,
    string Comment);
