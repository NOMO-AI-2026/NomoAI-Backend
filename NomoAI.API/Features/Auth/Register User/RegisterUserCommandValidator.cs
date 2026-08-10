using FluentValidation;
using NomoAI.API.Common.Enums;

namespace NomoAI.API.Features.Auth.Register_User;

public sealed class RegisterUserCommandValidator
    : AbstractValidator<RegisterUserCommand>
{
    private const int MaxClinicNameLength = 200;
    private const int MaxProfessionalBioLength = 2000;
    private const int MaxYearsOfExperience = 80;

    public RegisterUserCommandValidator()
    {
        When(command => command.Role == UserRole.Doctor, () =>
        {
            RuleFor(command => command.YearsOfExperience)
                .GreaterThanOrEqualTo(0)
                .When(command => command.YearsOfExperience.HasValue)
                .WithMessage("Years of experience cannot be negative.");

            RuleFor(command => command.YearsOfExperience)
                .LessThanOrEqualTo(MaxYearsOfExperience)
                .When(command => command.YearsOfExperience.HasValue)
                .WithMessage(
                    $"Years of experience cannot exceed {MaxYearsOfExperience}.");

            RuleFor(command => command.ClinicName)
                .MaximumLength(MaxClinicNameLength)
                .When(command => command.ClinicName is not null)
                .WithMessage(
                    $"Clinic name cannot exceed {MaxClinicNameLength} characters.");

            RuleFor(command => command.ProfessionalBio)
                .MaximumLength(MaxProfessionalBioLength)
                .When(command => command.ProfessionalBio is not null)
                .WithMessage(
                    $"Professional bio cannot exceed {MaxProfessionalBioLength} characters.");
        });
    }
}
