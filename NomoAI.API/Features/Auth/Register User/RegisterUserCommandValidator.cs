using FluentValidation;
using NomoAI.API.Common.DoctorDocuments;
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

            // URLs are produced server-side from uploaded files, not supplied by the client.
            RuleFor(command => command.IdentityDocumentUrl)
                .NotEmpty()
                .WithMessage("Identity document is required for doctor registration.")
                .MaximumLength(DoctorDocumentLimits.MaxUrlLength);

            RuleFor(command => command.PracticeLicenseUrl)
                .NotEmpty()
                .WithMessage("Practice license is required for doctor registration.")
                .MaximumLength(DoctorDocumentLimits.MaxUrlLength);

            RuleFor(command => command.SyndicateCardUrl)
                .NotEmpty()
                .WithMessage("Syndicate card is required for doctor registration.")
                .MaximumLength(DoctorDocumentLimits.MaxUrlLength);

            RuleFor(command => command.SyndicateRegistrationNumber)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .WithMessage("Syndicate registration number is required for doctor registration.")
                .MaximumLength(DoctorDocumentLimits.MaxSyndicateRegistrationNumberLength)
                .WithMessage(
                    $"Syndicate registration number cannot exceed {DoctorDocumentLimits.MaxSyndicateRegistrationNumberLength} characters.");
        });
    }
}
