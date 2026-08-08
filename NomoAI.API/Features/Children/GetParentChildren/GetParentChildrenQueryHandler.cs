using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Children.GetParentChildren
{
    internal sealed class GetParentChildrenQueryHandler : IRequestHandler<GetParentChildrenQuery, Result<PaginatedList<ChildrenResponse>>>
    {
        private readonly AppDbContext _db;
        public GetParentChildrenQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<PaginatedList<ChildrenResponse>>> Handle(GetParentChildrenQuery request, CancellationToken cancellationToken)
        {
            var parentId = await _db.Parents
                .Where(p => p.UserId == request.UserId && !p.IsDeleted)
                .Select(p => p.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (parentId == 0)
            {
                return Result.Failure<PaginatedList<ChildrenResponse>>(new Error("Children.ParentNotFound", "Parent not found.", 404));
            }

            var query = _db.Children
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.ParentId == parentId && (request.Name == null || c.FullName.Contains(request.Name)))
                .Select(c => new ChildrenResponse
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Gender = c.Gender,
                    Age = c.Age,
                    DoctorName = c.Doctor.User.Fullname
                })
                .OrderBy(c => c.FullName)
                .AsQueryable();

            var paginated = await PaginatedList<ChildrenResponse>.CreateAsync(query, request.PageNumber ?? 1, request.PageSize ?? 10);

            return Result.Success(paginated);
        }
    }
}
