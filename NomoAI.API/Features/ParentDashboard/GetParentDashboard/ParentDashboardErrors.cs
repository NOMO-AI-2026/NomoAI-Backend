using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.ParentDashboard.GetParentDashboard;

public static class ParentDashboardErrors
{
    public static readonly Error ParentNotFound = new(
        "ParentDashboard.ParentNotFound",
        "Parent not found.",
        StatusCodes.Status404NotFound);
}
