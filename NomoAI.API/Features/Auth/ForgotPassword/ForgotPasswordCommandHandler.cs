using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Email;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordHandler
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailOtpDispatcher _emailOtpDispatcher;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(
        UserManager<ApplicationUser> userManager,
        IEmailOtpDispatcher emailOtpDispatcher,
        ILogger<ForgotPasswordHandler> logger)
    {
        _userManager = userManager;
        _emailOtpDispatcher = emailOtpDispatcher;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        string email =
            request.Email.Trim();

        ApplicationUser? user =
            await _userManager.FindByEmailAsync(email);

        // Always return success to prevent account enumeration.
        if (user is null ||
            user.IsDeleted ||
            !user.EmailConfirmed ||
            string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Success();
        }

        Result<EmailOtpDispatchResult> dispatchResult =
            await _emailOtpDispatcher.SendAsync(
                user.Id,
                user.Email,
                EmailOtpPurpose.ResetPassword,
                cancellationToken);

        if (dispatchResult.IsFailure)
        {
            if (dispatchResult.Error.Code ==
                EmailOtpErrors.ResendTooSoon.Code)
            {
                _logger.LogInformation(
                    "Password reset OTP request was ignored " +
                    "because resend cooldown is active for " +
                    "user {UserId}.",
                    user.Id);
            }
            else if (dispatchResult.Error.Code !=
                AuthErrors.EmailDeliveryFailed.Code)
            {
                _logger.LogError(
                    "Failed to create password reset OTP " +
                    "for user {UserId}. ErrorCode: {ErrorCode}.",
                    user.Id,
                    dispatchResult.Error.Code);
            }

            // Delivery failures are already logged by EmailOtpDispatcher.
        }

        return Result.Success();
    }
}
