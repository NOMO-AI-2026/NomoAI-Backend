using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Payment.GetAllPaymentMethods
{
    internal sealed class GetAllPaymentMethodsQueryHandler
        : IRequestHandler<GetAllPaymentMethodsQuery, Result<IEnumerable<PaymentMethodResponse>>>
    {
        private readonly AppDbContext _db;

        public GetAllPaymentMethodsQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<IEnumerable<PaymentMethodResponse>>> Handle(
            GetAllPaymentMethodsQuery request,
            CancellationToken cancellationToken)
        {
            var methods = await _db.PaymentMethods
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .Select(m => new PaymentMethodResponse
                {
                    Id = m.Id,
                    Name = m.Name,
                    PaymentMethodType = m.PaymentMethodType
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<PaymentMethodResponse>>(methods);
        }
    }
}
