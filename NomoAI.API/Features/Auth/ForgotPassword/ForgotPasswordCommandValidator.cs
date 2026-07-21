using FluentValidation;

namespace NomoAI.API.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordValidator
    : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage(
                "Email address is required.")
            .EmailAddress()
            .WithMessage(
                "Email address is invalid.")
            .MaximumLength(256)
            .WithMessage(
                "Email address cannot exceed 256 characters.");
    }
}