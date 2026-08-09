using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Payment.GetAllPaymentMethods
{
    public class PaymentMethodResponse
    {
        public required string Id { get; set; }

        public string? Name { get; set; }

        public PaymentMethods PaymentMethodType { get; set; }
    }
}
