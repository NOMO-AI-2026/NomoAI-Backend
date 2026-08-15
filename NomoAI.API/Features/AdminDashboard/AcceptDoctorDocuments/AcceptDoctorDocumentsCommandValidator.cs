using FluentValidation;

namespace NomoAI.API.Features.AdminDashboard.AcceptDoctorDocuments;

public sealed class AcceptDoctorDocumentsCommandValidator
    : AbstractValidator<AcceptDoctorDocumentsCommand>
{
    public AcceptDoctorDocumentsCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");
    }
}
