using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordHandler
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly IEmailOtpService
        _emailOtpService;

    private readonly IEmailSender
        _emailSender;

    private readonly ILogger<ForgotPasswordHandler>
        _logger;

    public ForgotPasswordHandler(
        UserManager<ApplicationUser> userManager,
        IEmailOtpService emailOtpService,
        IEmailSender emailSender,
        ILogger<ForgotPasswordHandler> logger)
    {
        _userManager = userManager;
        _emailOtpService = emailOtpService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        string email =
            request.Email.Trim();

        ApplicationUser? user =
            await _userManager.FindByEmailAsync(
                email);

        
        if (user is null ||
            user.IsDeleted ||
            !user.EmailConfirmed ||
            string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Success();
        }

        Result<EmailOtpCreated> otpResult =
            await _emailOtpService.CreateAsync(
                user.Id,
                user.Email,
                EmailOtpPurpose.ResetPassword,
                cancellationToken);

        
        if (otpResult.IsFailure)
        {
            if (otpResult.Error.Code ==
                EmailOtpErrors.ResendTooSoon.Code)
            {
                _logger.LogInformation(
                    "Password reset OTP request was ignored " +
                    "because resend cooldown is active for " +
                    "user {UserId}.",
                    user.Id);
            }
            else
            {
                _logger.LogError(
                    "Failed to create password reset OTP " +
                    "for user {UserId}. ErrorCode: {ErrorCode}.",
                    user.Id,
                    otpResult.Error.Code);
            }

            return Result.Success();
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
            BuildResetPasswordEmail(
                otp.Code,
                expirationMinutes);

        try
        {
            await _emailSender.SendAsync(
                user.Email,
                "Your NomoAI password reset code",
                htmlBody,
                cancellationToken);

            _logger.LogInformation(
                "Password reset OTP was sent successfully " +
                "for user {UserId}.",
                user.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            
            _logger.LogError(
                exception,
                "Failed to send password reset OTP " +
                "for user {UserId}.",
                user.Id);
        }

        return Result.Success();
    }

    private static string BuildResetPasswordEmail(
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
                        Reset your password
                    </h2>

                    <p style="
                        color:#374151;
                        line-height:1.7;">
                        Use the following verification code
                        to reset your NomoAI password.
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
                        If you did not request a password reset,
                        you can safely ignore this email.
                    </p>
                </div>
            </body>
            </html>
            """;
    }
}