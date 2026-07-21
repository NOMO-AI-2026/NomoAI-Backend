using FluentValidation;

namespace NomoAI.API.Features.Auth.ResendEmailConfirmation;

public sealed class ResendEmailConfirmationValidator
    : AbstractValidator<ResendEmailConfirmationCommand>
{
    public ResendEmailConfirmationValidator()
    {
        RuleFor(command => command.UserId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage("User ID is required.")
            .Must(userId =>
                Guid.TryParse(userId, out _))
            .WithMessage("User ID is invalid.");
    }
}