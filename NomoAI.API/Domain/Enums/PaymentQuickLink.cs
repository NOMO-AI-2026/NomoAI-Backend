using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Domain.Enums
{
    public class PaymentQuickLink:BaseEntity<int>
    {
        public int PaymentId {  get; set; }
        public required string IdempotencyCode { get; set; }

        public string? ClientUrl { get; set; }
        public string? ShortenUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public Payment payment { get; set; }
    }
}
