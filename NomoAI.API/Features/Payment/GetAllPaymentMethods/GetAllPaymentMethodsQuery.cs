using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Payment.GetAllPaymentMethods
{
    public record GetAllPaymentMethodsQuery()
        : IRequest<Result<IEnumerable<PaymentMethodResponse>>>;
}
