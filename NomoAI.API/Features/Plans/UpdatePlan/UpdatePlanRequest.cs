using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Plans.UpdatePlan
{
    public class UpdatePlanRequest
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public int IncludedMinutes { get; set; }

        public decimal Price { get; set; }

        public MoneyCurrency Currency { get; set; } = MoneyCurrency.EGP;
    }
}
