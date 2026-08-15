using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.AdminDashboard.GetDoctorDetails;

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
        DoctorDetailsResponse? doctor = await _db.Doctor
            .AsNoTracking()
            .Where(item =>
                item.UserId == request.UserId &&
                !item.IsDeleted &&
                !item.User.IsDeleted)
            .Select(item => new DoctorDetailsResponse
            {
                Id = item.Id,
                UserId = item.UserId,
                FullName = item.User.Fullname,
                Email = item.User.Email ?? string.Empty,
                PhoneNumber = item.User.PhoneNumber,
                Gender = item.User.Gender,
                Age = item.User.Age,
                IsApproved = item.IsApproved,
                YearsOfExperience = item.YearsOfExperience,
                ClinicName = item.ClinicName,
                ProfessionalBio = item.ProfessionalBio,
                IdentityDocumentUrl = item.IdentityDocumentUrl,
                PracticeLicenseUrl = item.PracticeLicenseUrl,
                SyndicateCardUrl = item.SyndicateCardUrl,
                SyndicateRegistrationNumber = item.SyndicateRegistrationNumber,
                CreatedAt = item.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (doctor is null)
        {
            return Result.Failure<DoctorDetailsResponse>(
                AdminDashboardErrors.DoctorNotFound);
        }

        return Result.Success(doctor);
    }
}
