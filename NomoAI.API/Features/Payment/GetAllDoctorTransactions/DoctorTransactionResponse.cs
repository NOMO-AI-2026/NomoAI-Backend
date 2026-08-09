using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Payment.GetAllDoctorTransactions
{
    public sealed class DoctorTransactionResponse
    {
        public int Id { get; set; }

        public TransactionType Type { get; set; }

        public int Minutes { get; set; }

        public int BalanceAfter { get; set; }

        public int? PlanPurchaseId { get; set; }

        public int? SessionId { get; set; }

        public DateTime CreatedAt { get; set; }

        public PlanPurchaseSummaryResponse? PlanPurchase { get; set; }

        public SessionSummaryResponse? Session { get; set; }
    }

    public sealed class PlanPurchaseSummaryResponse
    {
        public int Id { get; set; }

        public int PlanId { get; set; }

        public string? PlanName { get; set; }

        public int PaymentId { get; set; }

        public int PurchasedMinutes { get; set; }

        public decimal PurchasedPrice { get; set; }

        public DateTime PurchasedAtUtc { get; set; }
    }

    public sealed class SessionSummaryResponse
    {
        public int Id { get; set; }

        public string ChildFullName { get; set; } = null!;

        public int ActivityId { get; set; }

        public string SessionTitle { get; set; } = null!;

        public SessionStatus Status { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }
    }
}
