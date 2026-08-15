using FluentValidation;
using NomoAI.API.Common.DoctorDocuments;

namespace NomoAI.API.Features.Profile.UpdateDoctorDocuments;

public sealed class UpdateDoctorDocumentsCommandValidator
    : AbstractValidator<UpdateDoctorDocumentsCommand>
{
    public UpdateDoctorDocumentsCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.PracticeLicense)
            .Custom((file, context) =>
            {
                string? error = DoctorDocumentFileRules.Validate(file, "Practice license");
                if (error is not null)
                {
                    context.AddFailure("practiceLicense", error);
                }
            });

        RuleFor(command => command.SyndicateCard)
            .Custom((file, context) =>
            {
                string? error = DoctorDocumentFileRules.Validate(file, "Syndicate card");
                if (error is not null)
                {
                    context.AddFailure("syndicateCard", error);
                }
            });
    }
}
