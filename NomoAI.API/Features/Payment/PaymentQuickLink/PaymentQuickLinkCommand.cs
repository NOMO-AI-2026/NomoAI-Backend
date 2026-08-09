using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Payment.PaymentQuickLink
{
    public record PaymentQuickLinkCommand(
        string DoctorUserId,
        string PaymentMethodId,
        int PlanId,
        string Idempotency,
        decimal PriceInEGP) : IRequest<Result<PaymentQuickLinkResponse>>;
}
