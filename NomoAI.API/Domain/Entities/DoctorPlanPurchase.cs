using System.Numerics;

namespace NomoAI.API.Domain.Entities
{
    public class DoctorPlanPurchase:BaseEntity<int>
    {
        public int DoctorId { get; set; }

        public int PlanId { get; set; }

        public int PaymentId { get; set; }

        public int PurchasedMinutes { get; set; }

        public decimal PurchasedPrice { get; set; }

        public DateTime PurchasedAtUtc { get; set; }

        public Doctor Doctor { get; set; } = null!;
        public SupscriptionPlan Plan { get; set; } = null!;
        public Payment Payment { get; set; } = null!;
    }
}
