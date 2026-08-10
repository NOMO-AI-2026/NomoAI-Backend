using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Activities.GetChildActivityList
{
    internal sealed class GetChildActivityListHandler : IRequestHandler<GetChildActivityListQuery, Result<IEnumerable<ActivityResponseDto>>>
    {
        private readonly AppDbContext _db;

        public GetChildActivityListHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<IEnumerable<ActivityResponseDto>>> Handle(GetChildActivityListQuery request, CancellationToken cancellationToken)
        {
            IQueryable<Domain.Entities.Activity> query = _db.Activities
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.ChildId == request.ChildId && (request.OnlyAvailableForSession==null ||
                request.OnlyAvailableForSession==a.CanMakeSession));

            // Session-start picker only — manage/history UIs keep seeing used activities.

            var activities = await query
                .Select(a => new ActivityResponseDto
                {
                    Id = a.Id,
                    ActivityTarget = a.ActivityTarget,
                    Content = a.Content,
                    EstimatedDurationMinutes = a.EstimatedDurationMinutes,
                    CanMakeSession = a.CanMakeSession,

                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<ActivityResponseDto>>(activities);
        }
    }
}
