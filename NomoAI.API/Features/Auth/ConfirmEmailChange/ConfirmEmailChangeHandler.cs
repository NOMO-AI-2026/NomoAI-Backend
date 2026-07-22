using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Email;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ConfirmEmailChange;

public sealed class ConfirmEmailChangeHandler
    : IRequestHandler<ConfirmEmailChangeCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailOtpService _emailOtpService;
    private readonly IEmailTemplateBuilder _emailTemplateBuilder;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ConfirmEmailChangeHandler> _logger;

    public ConfirmEmailChangeHandler(
        UserManager<ApplicationUser> userManager,
        IEmailOtpService emailOtpService,
        IEmailTemplateBuilder emailTemplateBuilder,
        IEmailSender emailSender,
        ILogger<ConfirmEmailChangeHandler> logger)
    {
        _userManager = userManager;
        _emailOtpService = emailOtpService;
        _emailTemplateBuilder = emailTemplateBuilder;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ConfirmEmailChangeCommand request,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user =
            await _userManager.FindByIdAsync(request.UserId);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(AuthErrors.UnauthorizedAccess);
        }

        Result<VerifiedEmailOtp> otpResult =
            await _emailOtpService.VerifyAndReserveAsync(
                user.Id,
                request.Otp.Trim(),
                EmailOtpPurpose.ChangeEmail,
                cancellationToken);

        if (otpResult.IsFailure)
        {
            return Result.Failure(otpResult.Error);
        }

        VerifiedEmailOtp verifiedOtp =
            otpResult.Value;

        string newEmail =
            verifiedOtp.TargetEmail.Trim();

        if (string.Equals(
            user.Email,
            newEmail,
            StringComparison.OrdinalIgnoreCase) &&
            user.EmailConfirmed)
        {
            await TryConsumeOtpAsync(
                user.Id,
                verifiedOtp.ReservationToken,
                cancellationToken);

            return Result.Success();
        }

        ApplicationUser? existingUser =
            await _userManager.FindByEmailAsync(newEmail);

        if (existingUser is not null &&
            existingUser.Id != user.Id)
        {
            await TryConsumeOtpAsync(
                user.Id,
                verifiedOtp.ReservationToken,
                cancellationToken);

            return Result.Failure(AuthErrors.EmailAlreadyInUse);
        }

        string? oldEmail =
            user.Email;

        string identityToken =
            await _userManager.GenerateChangeEmailTokenAsync(
                user,
                newEmail);

        IdentityResult changeResult =
            await _userManager.ChangeEmailAsync(
                user,
                newEmail,
                identityToken);

        if (!changeResult.Succeeded)
        {
            await _emailOtpService.ReleaseAsync(
                user.Id,
                EmailOtpPurpose.ChangeEmail,
                verifiedOtp.ReservationToken,
                cancellationToken);

            _logger.LogWarning(
                "Changing email failed for user {UserId}. Errors: {Errors}",
                user.Id,
                string.Join(
                    ", ",
                    changeResult.Errors.Select(error =>
                        $"{error.Code}: {error.Description}")));

            return Result.Failure(
                AuthErrors.EmailChangeFailed(
                    "The email address could not be changed."));
        }

        await TryConsumeOtpAsync(
            user.Id,
            verifiedOtp.ReservationToken,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldEmail) &&
            !string.Equals(
                oldEmail,
                newEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            await TrySendOldEmailNotificationAsync(
                oldEmail,
                newEmail,
                user.Id,
                cancellationToken);
        }

        _logger.LogInformation(
            "Email was changed successfully using OTP for user {UserId}.",
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
                EmailOtpPurpose.ChangeEmail,
                reservationToken,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The change-email operation completed, but " +
                "the OTP could not be consumed for user {UserId}.",
                userId);
        }
    }

    private async Task TrySendOldEmailNotificationAsync(
        string oldEmail,
        string newEmail,
        string userId,
        CancellationToken cancellationToken)
    {
        EmailMessage message =
            _emailTemplateBuilder.BuildEmailChangedNotification(
                newEmail);

        try
        {
            await _emailSender.SendAsync(
                oldEmail,
                message.Subject,
                message.HtmlBody,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Email-change notification could not be sent " +
                "to the old email for user {UserId}.",
                userId);
        }
    }
}
