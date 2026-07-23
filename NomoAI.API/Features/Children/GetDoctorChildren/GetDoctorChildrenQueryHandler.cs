using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
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
           // Console.WriteLine($"User Id : {request.UserId}");
           // int DoctorId = _db.Doctor.Where(x=> x.UserId == request.UserId).Select(x => x.Id).FirstOrDefault();
            Doctor isDoctorExist = await _db.Doctor.Where(x => !x.IsDeleted && x.UserId==request.UserId).FirstOrDefaultAsync();
            if (isDoctorExist==null) {
                return Result.Failure<IEnumerable<ChildrenResponse>>(ChildrenErrors.DoctorNotFound);
            }
            int DoctorId = isDoctorExist.Id;
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
