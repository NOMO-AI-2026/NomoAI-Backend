using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.AssignChildToParent;

public static class AssignChildToParentErrors
{
    public static readonly Error DoctorProfileNotFound = new(
        "Children.DoctorProfileNotFound",
        "Doctor profile was not found.",
        404);

    public static readonly Error DoctorNotApproved = new(
        "Children.DoctorNotApproved",
        "Your doctor account has not been approved yet.",
        403);

    public static readonly Error ChildNotFound = new(
        "Children.ChildNotFound",
        "Child not found.",
        404);

    public static readonly Error ParentNotFound = new(
        "Children.ParentNotFound",
        "Parent not found.",
        404);

    public static readonly Error ChildDoesNotBelongToDoctor = new(
        "Children.ChildDoesNotBelongToDoctor",
        "You do not have permission to assign this child to a parent.",
        403);
}