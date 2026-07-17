using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Activities.DeleteActivity;

public static class DeleteActivityErrors
{
    public static readonly Error ActivityNotFound = new(
        "Activities.ActivityNotFound",
        "Activity not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error UnauthorizedActivityAccess = new(
        "Activities.UnauthorizedActivityAccess",
        "You do not have permission to delete this activity.",
        StatusCodes.Status403Forbidden);

    public static readonly Error DoctorAccountNotAvailable = new(
        "Activities.DoctorAccountNotAvailable",
        "The doctor account is deleted or has not been approved.",
        StatusCodes.Status403Forbidden);

    public static readonly Error DeleteFailed = new(
        "Activities.DeleteFailed",
        "The activity could not be deleted.",
        StatusCodes.Status409Conflict);
}