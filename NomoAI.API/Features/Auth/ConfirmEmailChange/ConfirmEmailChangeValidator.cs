using FluentValidation;

namespace NomoAI.API.Features.Auth.ConfirmEmailChange;

public sealed class ConfirmEmailChangeValidator
    : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage(
                "User ID is required.");

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

        RuleFor(command => command.Token)
            .NotEmpty()
            .WithMessage(
                "Email change token is required.");
    }
}