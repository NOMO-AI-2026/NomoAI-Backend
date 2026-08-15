using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.AdminDashboard.GetAllDoctors
{
    internal sealed class GetAllDoctorsQueryHandler : IRequestHandler<GetAllDoctorsQuery, Result<PaginatedList<DoctorResponse>>>
    {
        private readonly AppDbContext _db;
        public GetAllDoctorsQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<PaginatedList<DoctorResponse>>> Handle(GetAllDoctorsQuery request, CancellationToken cancellationToken)
        {
            var query = _db.Doctor
                .AsNoTracking()
                .Where(d => !d.IsDeleted && (request.IsApproved == null || d.IsApproved == request.IsApproved) && 
                 (request.Name == null || d.User.Fullname.Contains(request.Name)))
                .Select(d => new DoctorResponse
                {
                    UserId = d.UserId,
                    FullName = d.User.Fullname,
                    Email = d.User.Email ?? string.Empty,
                    IsApproved = d.IsApproved,
                    YearsOfExperience = d.YearsOfExperience,
                    ClinicName = d.ClinicName,
                    ProfessionalBio = d.ProfessionalBio,
                    PracticeLicenseUrl = d.PracticeLicenseUrl,
                    SyndicateCardUrl = d.SyndicateCardUrl,
                })
                .OrderBy(d => d.FullName)
                .AsQueryable();

            var paginated = await PaginatedList<DoctorResponse>.CreateAsync(query, request.PageNumber, request.PageSize);
            return Result.Success(paginated);
        }
    }
}
