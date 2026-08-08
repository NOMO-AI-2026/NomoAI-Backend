using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Plans.AddPlan
{
    internal sealed class AddPlanCommandHandler : IRequestHandler<AddPlanCommand, Result<int>>
    {
        private readonly AppDbContext _db;

        public AddPlanCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<int>> Handle(AddPlanCommand request, CancellationToken cancellationToken)
        {
            var name = request.Name.Trim();

            var nameExists = await _db.SupscriptionPlan
                .AsNoTracking()
                .AnyAsync(
                    p => p.Name == name && !p.IsDeleted,
                    cancellationToken);

            if (nameExists)
            {
                return Result.Failure<int>(PlansErrors.NameAlreadyExists);
            }

            var plan = new SupscriptionPlan
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                IncludedMinutes = request.IncludedMinutes,
                Price = request.Price,
                Currency = request.Currency
            };

            _db.SupscriptionPlan.Add(plan);
            await _db.SaveChangesAsync(cancellationToken);

            return Result.Success(plan.Id);
        }
    }
}
