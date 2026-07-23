using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ConfirmEmail;

public sealed class ConfirmEmailHandler
    : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailOtpService _emailOtpService;
    private readonly ILogger<ConfirmEmailHandler> _logger;

    public ConfirmEmailHandler(
        UserManager<ApplicationUser> userManager,
        IEmailOtpService emailOtpService,
        ILogger<ConfirmEmailHandler> logger)
    {
        _userManager = userManager;
        _emailOtpService = emailOtpService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ConfirmEmailCommand request,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user =
            await _userManager.FindByIdAsync(request.UserId);

        // Do not reveal whether the user id is missing or the OTP is invalid.
        if (user is null || user.IsDeleted)
        {
            return Result.Failure(AuthErrors.InvalidToken);
        }

        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Failure(AuthErrors.InvalidToken);
        }

        Result<VerifiedEmailOtp> otpResult =
            await _emailOtpService.VerifyAndReserveAsync(
                user.Id,
                request.Otp,
                EmailOtpPurpose.ConfirmEmail,
                cancellationToken);

        if (otpResult.IsFailure)
        {
            return Result.Failure(otpResult.Error);
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
                EmailOtpPurpose.ConfirmEmail,
                verifiedOtp.ReservationToken,
                cancellationToken);

            _logger.LogWarning(
                "Confirmation OTP target email does not match " +
                "the current email for user {UserId}.",
                user.Id);

            return Result.Failure(AuthErrors.InvalidToken);
        }

        string identityToken =
            await _userManager.GenerateEmailConfirmationTokenAsync(
                user);

        IdentityResult confirmResult =
            await _userManager.ConfirmEmailAsync(
                user,
                identityToken);

        if (!confirmResult.Succeeded)
        {
            await _emailOtpService.ReleaseAsync(
                user.Id,
                EmailOtpPurpose.ConfirmEmail,
                verifiedOtp.ReservationToken,
                cancellationToken);

            _logger.LogWarning(
                "Email confirmation failed for user {UserId}. Errors: {Errors}",
                user.Id,
                string.Join(
                    ", ",
                    confirmResult.Errors.Select(error =>
                        $"{error.Code}: {error.Description}")));

            return Result.Failure(AuthErrors.InvalidToken);
        }

        await TryConsumeOtpAsync(
            user.Id,
            verifiedOtp.ReservationToken,
            cancellationToken);

        _logger.LogInformation(
            "Email was confirmed successfully using OTP for user {UserId}.",
            user.Id);

        return Result.Success();
    }

    private async Task TryConsumeOtpAsync(
        string userId,
        string reservationToken,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailOtpService.ConsumeAsync(
                userId,
                EmailOtpPurpose.ConfirmEmail,
                reservationToken,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Email was confirmed, but its OTP could not " +
                "be consumed for user {UserId}.",
                userId);
        }
    }
}
