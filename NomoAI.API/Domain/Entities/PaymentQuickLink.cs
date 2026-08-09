namespace NomoAI.API.Domain.Entities
{
    public class PaymentQuickLink : BaseEntity<int>
    {
        public int PaymentId { get; set; }

        public required string Idempotency { get; set; }

        public string? shortUrl { get; set; }

        public string? clientUrl { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public Payment Payment { get; set; } = null!;
    }
}
