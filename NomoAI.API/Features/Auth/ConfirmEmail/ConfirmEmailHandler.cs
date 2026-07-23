using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ConfirmEmail;

public sealed class ConfirmEmailHandler
    : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly IEmailOtpService
        _emailOtpService;

    private readonly ILogger<ConfirmEmailHandler>
        _logger;

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
            await _userManager.FindByIdAsync(
                request.UserId);


        if (user is null || user.IsDeleted)
        {
            return Result.Failure(
                AuthErrors.InvalidToken);
        }

        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Failure(
                AuthErrors.InvalidToken);
        }

       
        Result<Common.EmailOtp.VerifiedEmailOtp>
            otpResult =
                await _emailOtpService
                    .VerifyAndReserveAsync(
                        user.Id,
                        request.Otp,
                        EmailOtpPurpose.ConfirmEmail,
                        cancellationToken);

        if (otpResult.IsFailure)
        {
            return Result.Failure(
                otpResult.Error);
        }

        Common.EmailOtp.VerifiedEmailOtp verifiedOtp =
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

            return Result.Failure(
                AuthErrors.InvalidToken);
        }

        
        string identityToken =
            await _userManager
                .GenerateEmailConfirmationTokenAsync(
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

            IdentityError[] identityErrors =
                confirmResult.Errors.ToArray();

            _logger.LogWarning(
                "Email confirmation failed for user {UserId}. " +
                "Errors: {Errors}",
                user.Id,
                string.Join(
                    ", ",
                    identityErrors.Select(error =>
                        $"{error.Code}: " +
                        $"{error.Description}")));

            return Result.Failure(
                AuthErrors.InvalidToken);
        }

        try
        {
            await _emailOtpService.ConsumeAsync(
                user.Id,
                EmailOtpPurpose.ConfirmEmail,
                verifiedOtp.ReservationToken,
                cancellationToken);
        }
        catch (Exception exception)
        {
            
            _logger.LogError(
                exception,
                "Email was confirmed, but its OTP could not " +
                "be consumed for user {UserId}.",
                user.Id);
        }

        _logger.LogInformation(
            "Email was confirmed successfully using OTP " +
            "for user {UserId}.",
            user.Id);

        return Result.Success();
    }
}