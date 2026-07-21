using FluentValidation;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.EmailOtp;

namespace NomoAI.API.Features.Auth.ResetPassword;

public sealed class ResetPasswordValidator
    : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator(
        IOptions<EmailOtpOptions> otpOptions)
    {
        int otpLength =
            otpOptions.Value.Length;

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

        RuleFor(command => command.Otp)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage(
                "Verification code is required.")
            .Must(otp =>
            {
                string normalizedOtp =
                    otp.Trim();

                return
                    normalizedOtp.Length == otpLength &&
                    normalizedOtp.All(char.IsDigit);
            })
            .WithMessage(
                $"Verification code must contain exactly " +
                $"{otpLength} digits.");

        /*
         * لا نكرر Password Policy الخاصة بـ Identity هنا.
         *
         * Identity هي المسؤولة عن:
         * - Minimum length
         * - Uppercase
         * - Lowercase
         * - Digit
         * - Special character
         */
        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .WithMessage(
                "New password is required.");

        RuleFor(command => command.ConfirmNewPassword)
            .NotEmpty()
            .WithMessage(
                "Password confirmation is required.")
            .Equal(command => command.NewPassword)
            .WithMessage(
                "New password and confirmation password " +
                "do not match.");
    }
}