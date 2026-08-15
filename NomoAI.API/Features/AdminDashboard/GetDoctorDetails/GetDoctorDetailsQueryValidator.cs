using FluentValidation;

namespace NomoAI.API.Features.AdminDashboard.GetDoctorDetails;

public sealed class GetDoctorDetailsQueryValidator
    : AbstractValidator<GetDoctorDetailsQuery>
{
    public GetDoctorDetailsQueryValidator()
    {
        RuleFor(query => query.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");
    }
}
