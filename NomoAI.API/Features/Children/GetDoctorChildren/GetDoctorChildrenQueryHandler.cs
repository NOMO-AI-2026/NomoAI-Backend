using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Child;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Children.GetDoctorChildren
{
    internal sealed class GetDoctorChildrenQueryHandler : IRequestHandler<GetDoctorChildrenQuery, Result<PaginatedList<ChildrenResponse>>>
    {
        private readonly AppDbContext _db;

        public GetDoctorChildrenQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<PaginatedList<ChildrenResponse>>> Handle(GetDoctorChildrenQuery request, CancellationToken cancellationToken)
        {
            Doctor isDoctorExist = await _db.Doctor.Where(x => !x.IsDeleted && x.UserId == request.UserId).FirstOrDefaultAsync();
            if (isDoctorExist == null)
            {
                return Result.Failure<PaginatedList<ChildrenResponse>>(ChildrenErrors.DoctorNotFound);
            }
            int DoctorId = isDoctorExist.Id;

            var query = _db.Children
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.DoctorId == DoctorId && (request.Name == null || c.FullName.Contains(request.Name)))
                .Select(c => new ChildrenResponse
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Gender = c.Gender,
                    Age = c.Age
                })
                .OrderBy(c => c.FullName)
                .AsQueryable();

            var paginated = await PaginatedList<ChildrenResponse>.CreateAsync(query, request.PageNumber ?? 1, request.PageSize ?? 10);
            return Result.Success(paginated);
        }
    }
}
