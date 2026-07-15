using FluentValidation;

namespace NomoAI.API.Features.Auth.ResendEmailConfirmation;

public sealed class ResendEmailConfirmationValidator : AbstractValidator<ResendEmailConfirmationCommand>
{
    public ResendEmailConfirmationValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email address is invalid.")
            .MaximumLength(256)
            .WithMessage("Email address must not exceed 256 characters.");
    }
}