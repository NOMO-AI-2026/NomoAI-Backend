using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.AdminDashboard.GetDoctorDetails
{
    internal sealed class GetDoctorDetailsQueryHandler
        : IRequestHandler<GetDoctorDetailsQuery, Result<DoctorDetailsResponse>>
    {
        private readonly AppDbContext _db;

        public GetDoctorDetailsQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<DoctorDetailsResponse>> Handle(
            GetDoctorDetailsQuery request,
            CancellationToken cancellationToken)
        {
            var doctor = await _db.Doctor
                .AsNoTracking()
                .Where(d =>
                    d.UserId == request.UserId &&
                    !d.IsDeleted &&
                    !d.User.IsDeleted)
                .Select(d => new DoctorDetailsResponse
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    FullName = d.User.Fullname,
                    Email = d.User.Email ?? string.Empty,
                    PhoneNumber = d.User.PhoneNumber,
                    Age = d.User.Age,
                    Gender = d.User.Gender,
                    YearsOfExperience = d.YearsOfExperience,
                    ClinicName = d.ClinicName,
                    ProfessionalBio = d.ProfessionalBio,
                    IsApproved = d.IsApproved,
                    CreatedAt = d.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (doctor is null)
            {
                return Result.Failure<DoctorDetailsResponse>(
                    AdminDashboardErrors.DoctorNotFound);
            }

            return Result.Success(doctor);
        }
    }
}
