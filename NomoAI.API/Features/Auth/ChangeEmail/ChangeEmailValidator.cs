using FluentValidation;

namespace NomoAI.API.Features.Auth.ChangeEmail;

public sealed class ChangeEmailValidator
    : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage(
                "Authenticated user ID is required.");

        RuleFor(command => command.NewEmail)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage(
                "New email address is required.")
            .EmailAddress()
            .WithMessage(
                "New email address is invalid.")
            .MaximumLength(256)
            .WithMessage(
                "Email address cannot exceed 256 characters.");

        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .WithMessage(
                "Current password is required.");
    }
}