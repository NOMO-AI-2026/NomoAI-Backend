using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ResetPassword;

public sealed class ResetPasswordHandler
    : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly IEmailOtpService
        _emailOtpService;

    private readonly ILogger<ResetPasswordHandler>
        _logger;

    public ResetPasswordHandler(
        UserManager<ApplicationUser> userManager,
        IEmailOtpService emailOtpService,
        ILogger<ResetPasswordHandler> logger)
    {
        _userManager = userManager;
        _emailOtpService = emailOtpService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        string email =
            request.Email.Trim();

        ApplicationUser? user =
            await _userManager.FindByEmailAsync(
                email);

        
        if (user is null ||
            user.IsDeleted ||
            string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Failure(
                EmailOtpErrors.InvalidOrExpired);
        }

        Result<VerifiedEmailOtp> otpResult =
            await _emailOtpService
                .VerifyAndReserveAsync(
                    user.Id,
                    request.Otp.Trim(),
                    EmailOtpPurpose.ResetPassword,
                    cancellationToken);

        if (otpResult.IsFailure)
        {
            return Result.Failure(
                otpResult.Error);
        }

        VerifiedEmailOtp verifiedOtp =
            otpResult.Value;

        
        if (!string.Equals(
            verifiedOtp.TargetEmail,
            user.Email,
            StringComparison.OrdinalIgnoreCase))
        {
            await _emailOtpService.ReleaseAsync(
                user.Id,
                EmailOtpPurpose.ResetPassword,
                verifiedOtp.ReservationToken,
                cancellationToken);

            _logger.LogWarning(
                "Password reset OTP target email does not " +
                "match the current email for user {UserId}.",
                user.Id);

            return Result.Failure(
                EmailOtpErrors.InvalidOrExpired);
        }

        
        string identityToken =
            await _userManager
                .GeneratePasswordResetTokenAsync(
                    user);

        IdentityResult resetResult =
            await _userManager.ResetPasswordAsync(
                user,
                identityToken,
                request.NewPassword);

        if (!resetResult.Succeeded)
        {
           
            await _emailOtpService.ReleaseAsync(
                user.Id,
                EmailOtpPurpose.ResetPassword,
                verifiedOtp.ReservationToken,
                cancellationToken);

            string errors =
                string.Join(
                    " ",
                    resetResult.Errors.Select(
                        error => error.Description));

            _logger.LogWarning(
                "Password reset failed for user {UserId}. " +
                "Errors: {Errors}",
                user.Id,
                string.Join(
                    ", ",
                    resetResult.Errors.Select(error =>
                        $"{error.Code}: {error.Description}")));

            return Result.Failure(
                AuthErrors.PasswordResetFailed(
                    errors));
        }

       
        try
        {
            await _emailOtpService.ConsumeAsync(
                user.Id,
                EmailOtpPurpose.ResetPassword,
                verifiedOtp.ReservationToken,
                cancellationToken);
        }
        catch (Exception exception)
        {
            
            _logger.LogError(
                exception,
                "Password was reset, but its OTP could not " +
                "be consumed for user {UserId}.",
                user.Id);
        }

        _logger.LogInformation(
            "Password was reset successfully using OTP " +
            "for user {UserId}.",
            user.Id);

        return Result.Success();
    }
}