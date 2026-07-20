using FluentValidation;

namespace NomoAI.API.Features.Auth.ChangePassword;

public sealed class ChangePasswordValidator
    : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage(
                "Authenticated user ID is required.");

        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .WithMessage(
                "Current password is required.");

        RuleFor(command => command.NewPassword)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage(
                "New password is required.")
            .NotEqual(command => command.CurrentPassword)
            .WithMessage(
                "New password must be different from the current password.");

        RuleFor(command => command.ConfirmNewPassword)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage(
                "Password confirmation is required.")
            .Equal(command => command.NewPassword)
            .WithMessage(
                "New password and confirmation password do not match.");
    }
}