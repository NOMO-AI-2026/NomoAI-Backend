using FluentValidation;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.EmailOtp;

namespace NomoAI.API.Features.Auth.ConfirmEmailChange;

public sealed class ConfirmEmailChangeValidator
    : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeValidator(
        IOptions<EmailOtpOptions> otpOptions)
    {
        int otpLength =
            otpOptions.Value.Length;

        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

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
    }
}