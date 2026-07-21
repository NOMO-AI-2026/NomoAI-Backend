using FluentValidation;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.EmailOtp;

namespace NomoAI.API.Features.Auth.ConfirmEmail;

public sealed class ConfirmEmailValidator
    : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailValidator(
        IOptions<EmailOtpOptions> otpOptions)
    {
        int otpLength =
            otpOptions.Value.Length;

        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage(
                "User ID is required.");

        RuleFor(command => command.Otp)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage(
                "Verification code is required.")
            .Must(otp =>
                otp.Length == otpLength &&
                otp.All(char.IsDigit))
            .WithMessage(
                $"Verification code must contain exactly " +
                $"{otpLength} digits.");
    }
}