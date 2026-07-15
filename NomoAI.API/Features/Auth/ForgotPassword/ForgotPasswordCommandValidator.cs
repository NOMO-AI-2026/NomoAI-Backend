using FluentValidation;

namespace NomoAI.API.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandValidator:AbstractValidator<ForgotPasswordCommand>
{
	public ForgotPasswordCommandValidator()
	{
		RuleFor(command => command.Email)
			.NotEmpty()
			.WithMessage("Email is required.")
			.EmailAddress()
			.WithMessage("Email format is invalid.");
	}
}