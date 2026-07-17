using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Activities.UpdateActivity;

public static class UpdateActivityErrors
{
    public static readonly Error DoctorProfileNotFound = new(
        "Activities.Update.DoctorProfileNotFound",
        "Doctor profile was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error DoctorNotApproved = new(
        "Activities.Update.DoctorNotApproved",
        "The doctor account has not been approved.",
        StatusCodes.Status403Forbidden);

    public static readonly Error ActivityNotFound = new(
        "Activities.Update.ActivityNotFound",
        "Activity not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error UnauthorizedActivityAccess = new(
        "Activities.Update.UnauthorizedActivityAccess",
        "You do not have permission to update this activity.",
        StatusCodes.Status403Forbidden);

    public static readonly Error UpdateFailed = new(
        "Activities.Update.UpdateFailed",
        "The activity could not be updated.",
        StatusCodes.Status409Conflict);
}