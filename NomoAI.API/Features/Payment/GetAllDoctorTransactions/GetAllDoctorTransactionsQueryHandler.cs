using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Payment.GetAllDoctorTransactions
{
    internal sealed class GetAllDoctorTransactionsQueryHandler
        : IRequestHandler<GetAllDoctorTransactionsQuery, Result<PaginatedList<DoctorTransactionResponse>>>
    {
        private readonly AppDbContext _db;

        public GetAllDoctorTransactionsQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<PaginatedList<DoctorTransactionResponse>>> Handle(
            GetAllDoctorTransactionsQuery request,
            CancellationToken cancellationToken)
        {
            var doctor = await _db.Doctor
                .AsNoTracking()
                .Where(d => d.UserId == request.DoctorUserId && !d.IsDeleted)
                .Select(d => new { d.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (doctor is null)
            {
                return Result.Failure<PaginatedList<DoctorTransactionResponse>>(PaymentErrors.DoctorNotFound);
            }

            var query = _db.DoctorTransactions
                .AsNoTracking()
                .Where(t => t.DoctorId == doctor.Id && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new DoctorTransactionResponse
                {
                    Id = t.Id,
                    Type = t.Type,
                    Minutes = t.Minutes,
                    BalanceAfter = t.BalanceAfter,
                    PlanPurchaseId = t.PlanPurchaseId,
                    SessionId = t.SessionId,
                    CreatedAt = t.CreatedAt,
                    PlanPurchase = t.PlanPurchaseId == null || t.Plan!.IsDeleted
                        ? null
                        : new PlanPurchaseSummaryResponse
                        {
                            Id = t.Plan.Id,
                            PlanId = t.Plan.PlanId,
                            PlanName = t.Plan.Plan.Name,
                            PaymentId = t.Plan.PaymentId,
                            PurchasedMinutes = t.Plan.PurchasedMinutes,
                            PurchasedPrice = t.Plan.PurchasedPrice,
                            PurchasedAtUtc = t.Plan.PurchasedAtUtc
                        },
                    // Prefer purchase details when present; otherwise keep session details.
                    Session = t.PlanPurchaseId != null && !t.Plan!.IsDeleted
                        ? null
                        : t.SessionId == null || t.Session!.IsDeleted
                            ? null
                            : new SessionSummaryResponse
                            {
                                Id = t.Session.Id,
                                ChildFullName = t.Session.Child.FullName,
                                ActivityId = t.Session.ActivityId,
                                SessionTitle = t.Session.SessionTitle,
                                Status = t.Session.Status,
                                StartedAt = t.Session.StartedAt,
                                EndedAt = t.Session.EndedAt
                            }
                });

            var paginated = await PaginatedList<DoctorTransactionResponse>.CreateAsync(
                query,
                request.PageNumber,
                request.PageSize);

            return Result.Success(paginated);
        }
    }
}
