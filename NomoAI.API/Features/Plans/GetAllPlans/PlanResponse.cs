using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Plans.GetAllPlans
{
    public class PlanResponse
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public string? Description { get; set; }

        public int IncludedMinutes { get; set; }

        public decimal Price { get; set; }

        public MoneyCurrency Currency { get; set; }
    }
}
