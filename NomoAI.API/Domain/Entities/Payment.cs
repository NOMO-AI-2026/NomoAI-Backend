using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Domain.Entities
{
    public class Payment:BaseEntity<int>
    {
        public int DoctorId { get; set; }

        public decimal Amount { get; set; }

        public MoneyCurrency Currency { get; set; } = MoneyCurrency.EGP;

        public PaymentStatus Status { get; set; }

        public string ReferenceId { get; set; } = null!;

        public PaymentProvider Provider { get; set; } = PaymentProvider.Paymob;

        public DateTime? PaidAtUtc { get; set; }

        public Doctor Doctor { get; set; }

    }
}
