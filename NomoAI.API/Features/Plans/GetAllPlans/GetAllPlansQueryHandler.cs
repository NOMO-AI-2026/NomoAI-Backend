using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Plans.GetAllPlans
{
    internal sealed class GetAllPlansQueryHandler
        : IRequestHandler<GetAllPlansQuery, Result<IEnumerable<PlanResponse>>>
    {
        private readonly AppDbContext _db;

        public GetAllPlansQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<IEnumerable<PlanResponse>>> Handle(
            GetAllPlansQuery request,
            CancellationToken cancellationToken)
        {
            var plans = await _db.SupscriptionPlan
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Price)
                .Select(p => new PlanResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    IncludedMinutes = p.IncludedMinutes,
                    Price = p.Price,
                    Currency = p.Currency
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<PlanResponse>>(plans);
        }
    }
}
