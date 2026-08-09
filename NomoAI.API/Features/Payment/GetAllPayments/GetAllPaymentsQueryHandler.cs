using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Payment.GetAllPayments
{
    internal sealed class GetAllPaymentsQueryHandler
        : IRequestHandler<GetAllPaymentsQuery, Result<PaginatedList<PaymentListItemResponse>>>
    {
        private readonly AppDbContext _db;

        public GetAllPaymentsQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<PaginatedList<PaymentListItemResponse>>> Handle(
            GetAllPaymentsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _db.Payments
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PaymentListItemResponse
                {
                    Id = p.Id,
                    DoctorId = p.DoctorId,
                    Doctor = new DoctorSummaryResponse
                    {
                        Name = p.Doctor.User.Fullname,
                        Email = p.Doctor.User.Email,
                        Phone = p.Doctor.User.PhoneNumber
                    },
                    PaymentMethodId = p.PaymentMethodId,
                    PaymentMethodName = p.paymentMethod.Name,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    Status = p.Status,
                    Provider = p.Provider,
                    PaidAtUtc = p.PaidAtUtc,
                    CreatedAt = p.CreatedAt
                });

            var paginated = await PaginatedList<PaymentListItemResponse>.CreateAsync(
                query,
                request.PageNumber,
                request.PageSize);

            return Result.Success(paginated);
        }
    }
}
