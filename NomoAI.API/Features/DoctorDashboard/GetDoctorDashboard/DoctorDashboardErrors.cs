using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.DoctorDashboard.GetDoctorDashboard;

public static class DoctorDashboardErrors
{
    public static readonly Error DoctorNotFound = new(
        "DoctorDashboard.DoctorNotFound",
        "Doctor not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error DoctorNotApproved = new(
        "DoctorDashboard.DoctorNotApproved",
        "The doctor account has not been approved.",
        StatusCodes.Status403Forbidden);
}
