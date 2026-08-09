namespace NomoAI.API.Features.Payment.PaymentQuickLink
{
    public class PaymentQuickLinkRequest
    {
        public required string PaymentMethodId { get; set; }

        public required int PlanId { get; set; }

        public required string Idempotency { get; set; }

        public decimal PriceInEGP { get; set; }
    }
}
