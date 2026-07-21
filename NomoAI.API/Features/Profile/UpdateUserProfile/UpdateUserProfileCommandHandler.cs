using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Profile.UpdateUserProfile
{
    internal sealed class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<bool>>
    {
        private readonly AppDbContext _db;
        public UpdateUserProfileCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<bool>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Where(u => u.Id == request.UserId && !u.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return Result.Failure<bool>(ProfileErrors.UserNotFound);
            }

            // update basic user fields
            user.Fullname = request.Request.FullName;
            user.PhoneNumber = request.Request.PhoneNumber;
            user.Gender = request.Request.gender;
            user.Age = request.Request.Age;

            // check if user is a doctor
            var doctor = await _db.Doctor
                .Where(d => d.UserId == request.UserId && !d.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (doctor is not null && request.Request.DoctorSpecificData is not null)
            {
                doctor.YearsOfExperience = request.Request.DoctorSpecificData.YearsOfExperience;
                doctor.ClinicName = request.Request.DoctorSpecificData.ClinicName;
                doctor.ProfessionalBio = request.Request.DoctorSpecificData.ProfessionalBio;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return Result.Success(true);
        }
    }
}
