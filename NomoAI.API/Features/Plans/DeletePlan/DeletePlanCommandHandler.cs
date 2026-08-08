using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Plans.DeletePlan
{
    internal sealed class DeletePlanCommandHandler : IRequestHandler<DeletePlanCommand, Result>
    {
        private readonly AppDbContext _db;

        public DeletePlanCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result> Handle(DeletePlanCommand request, CancellationToken cancellationToken)
        {
            var planExists = await _db.SupscriptionPlan
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.PlanId && !p.IsDeleted, cancellationToken);

            if (!planExists)
            {
                return Result.Failure(PlansErrors.PlanNotFound);
            }

            var affected = await _db.SupscriptionPlan
                .Where(p => p.Id == request.PlanId && !p.IsDeleted)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(p => p.IsDeleted, true),
                    cancellationToken);

            if (affected == 0)
            {
                return Result.Failure(PlansErrors.DeleteFailed);
            }

            return Result.Success();
        }
    }
}
