using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Payment.GetAllDoctorTransactions
{
    public record GetAllDoctorTransactionsQuery(
        string DoctorUserId,
        int PageNumber,
        int PageSize) : IRequest<Result<PaginatedList<DoctorTransactionResponse>>>;
}
