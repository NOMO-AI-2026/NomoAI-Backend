using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ChangeEmail;

public sealed class ChangeEmailHandler
    : IRequestHandler<ChangeEmailCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailOtpService _emailOtpService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ChangeEmailHandler> _logger;

    public ChangeEmailHandler(
        UserManager<ApplicationUser> userManager,
        IEmailOtpService emailOtpService,
        IEmailSender emailSender,
        ILogger<ChangeEmailHandler> logger)
    {
        _userManager = userManager;
        _emailOtpService = emailOtpService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ChangeEmailCommand request,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                request.UserId);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(
                AuthErrors.UnauthorizedAccess);
        }

      
        bool passwordIsValid =
            await _userManager.CheckPasswordAsync(
                user,
                request.CurrentPassword);

        if (!passwordIsValid)
        {
            return Result.Failure(
                AuthErrors.IncorrectPassword);
        }

        string newEmail =
            request.NewEmail.Trim();

        string normalizedNewEmail =
            _userManager.NormalizeEmail(newEmail);

        if (string.Equals(
            user.NormalizedEmail,
            normalizedNewEmail,
            StringComparison.Ordinal))
        {
            return Result.Failure(
                AuthErrors.EmailUnchanged);
        }

        ApplicationUser? existingUser =
            await _userManager.FindByEmailAsync(
                newEmail);

        if (existingUser is not null &&
            existingUser.Id != user.Id)
        {
            return Result.Failure(
                AuthErrors.EmailAlreadyInUse);
        }

        
        Result<EmailOtpCreated> otpResult =
            await _emailOtpService.CreateAsync(
                user.Id,
                newEmail,
                EmailOtpPurpose.ChangeEmail,
                cancellationToken);

        if (otpResult.IsFailure)
        {
            return Result.Failure(
                otpResult.Error);
        }

        EmailOtpCreated otp =
            otpResult.Value;

        int expirationMinutes =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (otp.ExpiresAtUtc - DateTime.UtcNow)
                    .TotalMinutes));

        string htmlBody =
            BuildOtpEmailBody(
                otp.Code,
                expirationMinutes);

        try
        {
            await _emailSender.SendAsync(
                newEmail,
                "Confirm your new NomoAI email address",
                htmlBody,
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
                "Failed to send the change-email OTP " +
                "for user {UserId}.",
                user.Id);

            return Result.Failure(
                AuthErrors.EmailDeliveryFailed);
        }

        _logger.LogInformation(
            "Change-email OTP was sent successfully " +
            "for user {UserId}.",
            user.Id);

        return Result.Success();
    }

    private static string BuildOtpEmailBody(
        string otp,
        int expirationMinutes)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport"
                      content="width=device-width, initial-scale=1.0">
            </head>

            <body style="
                margin:0;
                padding:24px;
                background-color:#f5f7fa;
                font-family:Arial,sans-serif;">

                <div style="
                    max-width:600px;
                    margin:0 auto;
                    padding:32px;
                    background-color:#ffffff;
                    border:1px solid #e5e7eb;
                    border-radius:10px;">

                    <h2 style="
                        margin-top:0;
                        color:#111827;">
                        Confirm your new email address
                    </h2>

                    <p style="
                        color:#374151;
                        line-height:1.7;">
                        Use the following verification code
                        to confirm your new email address.
                    </p>

                    <div style="
                        margin:30px 0;
                        padding:20px;
                        background-color:#f3f4f6;
                        border-radius:8px;
                        text-align:center;">

                        <span style="
                            font-size:32px;
                            font-weight:bold;
                            letter-spacing:10px;
                            color:#111827;">
                            {otp}
                        </span>
                    </div>

                    <p style="
                        color:#374151;
                        line-height:1.7;">
                        This code expires in approximately
                        {expirationMinutes} minutes.
                    </p>

                    <p style="
                        color:#6b7280;
                        font-size:14px;
                        line-height:1.6;">
                        Never share this code with anyone.
                    </p>

                    <p style="
                        color:#6b7280;
                        font-size:14px;
                        line-height:1.6;">
                        If you did not request this change,
                        secure your account immediately.
                    </p>
                </div>
            </body>
            </html>
            """;
    }
}