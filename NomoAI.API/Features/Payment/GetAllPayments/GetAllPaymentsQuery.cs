using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Payment.GetAllPayments
{
    public record GetAllPaymentsQuery(int PageNumber, int PageSize)
        : IRequest<Result<PaginatedList<PaymentListItemResponse>>>;
}
