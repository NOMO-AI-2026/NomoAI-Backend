using FluentValidation;

namespace NomoAI.API.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandValidator: AbstractValidator<ResetPasswordCommand>
{
	public ResetPasswordCommandValidator()
	{
		RuleFor(command => command.UserId)
			.NotEmpty()
			.WithMessage("User ID is required.");

		RuleFor(command => command.Token)
			.NotEmpty()
			.WithMessage("Reset token is required.");

		RuleFor(command => command.NewPassword)
			.NotEmpty()
			.WithMessage("New password is required.");

		RuleFor(command => command.ConfirmPassword)
			.NotEmpty()
			.WithMessage("Password confirmation is required.")
			.Equal(command => command.NewPassword)
			.WithMessage("Passwords do not match.");
	}
}