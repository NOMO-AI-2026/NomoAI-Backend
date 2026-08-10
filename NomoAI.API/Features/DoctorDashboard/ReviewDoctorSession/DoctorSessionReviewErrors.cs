using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.DoctorDashboard.ReviewDoctorSession;

public static class DoctorSessionReviewErrors
{
    public static readonly Error SessionNotFound = new(
        "DoctorDashboard.SessionNotFound",
        "Session not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error SessionNotCompleted = new(
        "DoctorDashboard.SessionNotCompleted",
        "Only completed sessions can be reviewed.",
        StatusCodes.Status400BadRequest);

    public static readonly Error SessionAlreadyReviewed = new(
        "DoctorDashboard.SessionAlreadyReviewed",
        "This session has already been reviewed.",
        StatusCodes.Status409Conflict);

    public static readonly Error InsufficientCredit = new(
        "DoctorDashboard.InsufficientCredit",
        "You do not have enough credits to repeat this session.",
        StatusCodes.Status400BadRequest);
}
