using NomoAI.API.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace NomoAI.API.Domain.Entities
{
    public class DoctorTransaction : BaseEntity<int>
    {
        public int DoctorId { get; set; }

        public TransactionType Type { get; set; }

        public int Minutes { get; set; }

        public int BalanceAfter { get; set; }

        public int? PlanPurchaseId { get; set; }

        public int? SessionId { get; set; }
        [ForeignKey(nameof(PlanPurchaseId))]
        public DoctorPlanPurchase? Plan { get; set; } 

        public Session? Session { get; set; }

    }
}
