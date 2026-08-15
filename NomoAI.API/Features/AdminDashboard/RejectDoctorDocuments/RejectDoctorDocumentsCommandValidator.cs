using FluentValidation;

namespace NomoAI.API.Features.AdminDashboard.RejectDoctorDocuments;

public sealed class RejectDoctorDocumentsCommandValidator
    : AbstractValidator<RejectDoctorDocumentsCommand>
{
    public RejectDoctorDocumentsCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");
    }
}
