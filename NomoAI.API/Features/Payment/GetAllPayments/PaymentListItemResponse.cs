using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Payment.GetAllPayments
{
    public sealed class PaymentListItemResponse
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }

        public DoctorSummaryResponse Doctor { get; set; } = null!;

        public string PaymentMethodId { get; set; } = null!;

        public string? PaymentMethodName { get; set; }

        public decimal Amount { get; set; }

        public MoneyCurrency Currency { get; set; }

        public PaymentStatus Status { get; set; }

        public PaymentProvider Provider { get; set; }

        public DateTime? PaidAtUtc { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class DoctorSummaryResponse
    {
        public string Name { get; set; } = null!;

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }
}
