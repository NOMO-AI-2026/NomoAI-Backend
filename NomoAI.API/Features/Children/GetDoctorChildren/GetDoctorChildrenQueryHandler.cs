using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Features.Child;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Children.GetDoctorChildren
{
    internal sealed class GetDoctorChildrenQueryHandler : IRequestHandler<GetDoctorChildrenQuery, Result<IEnumerable<ChildrenResponse>>>
    {
        private readonly AppDbContext _db;

        public GetDoctorChildrenQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<IEnumerable<ChildrenResponse>>> Handle(GetDoctorChildrenQuery request, CancellationToken cancellationToken)
        {
            int DoctorId = _db.Doctor.Where(x=> x.UserId == request.UserId).Select(x => x.Id).FirstOrDefault();
            bool isDoctorExist = await _db.Doctor.AnyAsync(x => !x.IsDeleted && x.Id == DoctorId);
            if (!isDoctorExist) {
                return Result.Failure<IEnumerable<ChildrenResponse>>(ChildrenErrors.DoctorNotFound);
            }
            var children = await _db.Children
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.DoctorId == DoctorId)
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
