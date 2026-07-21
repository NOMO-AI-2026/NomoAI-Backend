using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Profile.GetUserProfile
{
    internal sealed class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileResponse>>
    {
        private readonly AppDbContext _db;
        public GetUserProfileQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<UserProfileResponse>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == request.UserId && !u.IsDeleted)
                .Select(u => new
                {
                    u.Id,
                    u.Fullname,
                    u.Email,
                    u.PhoneNumber,
                    u.Gender,
                    u.Age
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return Result.Failure<UserProfileResponse>(ProfileErrors.UserNotFound);
            }

            // check if doctor profile exists
            var doctor = await _db.Doctor
                .AsNoTracking()
                .Where(d => d.UserId == request.UserId && !d.IsDeleted)
                .Select(d => new DoctorData
                {
                    YearsOfExperience = d.YearsOfExperience,
                    ClinicName = d.ClinicName,
                    ProfessionalBio = d.ProfessionalBio
                })
                .SingleOrDefaultAsync(cancellationToken);

            var response = new UserProfileResponse
            {
                FullName = user.Fullname,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                gender = user.Gender,
                Age = user.Age,
                DoctorSpecificData = doctor
            };

            return Result.Success(response);
        }
    }
}
