using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Plans
{
    public static class PlansErrors
    {
        public static readonly Error NameAlreadyExists = new(
            "Plans.NameAlreadyExists",
            "A plan with this name already exists.",
            409);

        public static readonly Error PlanNotFound = new(
            "Plans.PlanNotFound",
            "Plan not found.",
            404);

        public static readonly Error UpdateFailed = new(
            "Plans.UpdateFailed",
            "Failed to update the plan.",
            400);

        public static readonly Error DeleteFailed = new(
            "Plans.DeleteFailed",
            "Failed to delete the plan.",
            400);
    }
}
