using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Plans.AddPlan
{
    public class AddPlanRequest
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public int IncludedMinutes { get; set; }

        public decimal Price { get; set; }

        public MoneyCurrency Currency { get; set; } = MoneyCurrency.EGP;
    }
}
