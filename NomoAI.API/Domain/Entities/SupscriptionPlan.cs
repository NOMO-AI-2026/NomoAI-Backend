using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Domain.Entities
{
    public class SupscriptionPlan:BaseEntity<int>
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public int IncludedMinutes { get; set; }

        public decimal Price { get; set; }

        public MoneyCurrency Currency { get; set; } = MoneyCurrency.EGP;
    }
}
