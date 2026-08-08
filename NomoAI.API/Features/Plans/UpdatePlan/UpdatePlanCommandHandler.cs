using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Plans.UpdatePlan
{
    internal sealed class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand, Result>
    {
        private readonly AppDbContext _db;

        public UpdatePlanCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
        {
            var planExists = await _db.SupscriptionPlan
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.PlanId && !p.IsDeleted, cancellationToken);

            if (!planExists)
            {
                return Result.Failure(PlansErrors.PlanNotFound);
            }

            var name = request.Name.Trim();
            var description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

            var nameExists = await _db.SupscriptionPlan
                .AsNoTracking()
                .AnyAsync(
                    p => p.Name == name && p.Id != request.PlanId && !p.IsDeleted,
                    cancellationToken);

            if (nameExists)
            {
                return Result.Failure(PlansErrors.NameAlreadyExists);
            }

            var affected = await _db.SupscriptionPlan
                .Where(p => p.Id == request.PlanId && !p.IsDeleted)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(p => p.Name, name)
                        .SetProperty(p => p.Description, description)
                        .SetProperty(p => p.IncludedMinutes, request.IncludedMinutes)
                        .SetProperty(p => p.Price, request.Price)
                        .SetProperty(p => p.Currency, request.Currency),
                    cancellationToken);

            if (affected == 0)
            {
                return Result.Failure(PlansErrors.UpdateFailed);
            }

            return Result.Success();
        }
    }
}
