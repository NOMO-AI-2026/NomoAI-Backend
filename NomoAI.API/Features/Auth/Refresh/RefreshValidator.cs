using FluentValidation;

namespace NomoAI.API.Features.Auth.Refresh;

public sealed class RefreshValidator
    : AbstractValidator<RefreshCommand>
{
    public RefreshValidator()
    {
        RuleFor(command => command.RefreshToken)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage(
                "Refresh token is required.")
            .Must(token => !string.IsNullOrWhiteSpace(token))
            .WithMessage(
                "Refresh token is required.")
            .MaximumLength(512)
            .WithMessage(
                "Refresh token is invalid.");
    }
}
