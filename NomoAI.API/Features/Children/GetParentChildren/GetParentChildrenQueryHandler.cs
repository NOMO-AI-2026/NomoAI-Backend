using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Children.GetParentChildren
{
    internal sealed class GetParentChildrenQueryHandler : IRequestHandler<GetParentChildrenQuery, Result<IEnumerable<ChildrenResponse>>>
    {
        private readonly AppDbContext _db;
        public GetParentChildrenQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<IEnumerable<ChildrenResponse>>> Handle(GetParentChildrenQuery request, CancellationToken cancellationToken)
        {
            var parentId = await _db.Parents
                .Where(p => p.UserId == request.UserId && !p.IsDeleted)
                .Select(p => p.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (parentId == 0)
            {
                return Result.Failure<IEnumerable<ChildrenResponse>>(new Error("Children.ParentNotFound", "Parent not found.", 404));
            }

            var children = await _db.Children
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.ParentId == parentId)
                .Select(c => new ChildrenResponse
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Gender = c.Gender,
                    Age = c.Age
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<ChildrenResponse>>(children);
        }
    }
}
