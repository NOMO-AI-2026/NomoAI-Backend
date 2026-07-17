using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Activities.CreateActivity;

public static class CreateActivityErrors
{
    public static readonly Error DoctorProfileNotFound = new(
        "Activities.DoctorProfileNotFound",
        "Doctor profile was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error DoctorNotApproved = new(
        "Activities.DoctorNotApproved",
        "The doctor account has not been approved.",
        StatusCodes.Status403Forbidden);

    public static readonly Error ChildNotFound = new(
        "Activities.ChildNotFound",
        "Child not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error ChildDoesNotBelongToDoctor = new(
        "Activities.ChildDoesNotBelongToDoctor",
        "You do not have permission to create an activity for this child.",
        StatusCodes.Status403Forbidden);
}