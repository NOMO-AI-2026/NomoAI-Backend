using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ResendEmailConfirmation;

public sealed class ResendEmailConfirmationHandler
    : IRequestHandler<ResendEmailConfirmationCommand, Result>
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly IEmailOtpService
        _emailOtpService;

    private readonly IEmailSender
        _emailSender;

    private readonly ILogger<ResendEmailConfirmationHandler>
        _logger;

    public ResendEmailConfirmationHandler(
        UserManager<ApplicationUser> userManager,
        IEmailOtpService emailOtpService,
        IEmailSender emailSender,
        ILogger<ResendEmailConfirmationHandler> logger)
    {
        _userManager = userManager;
        _emailOtpService = emailOtpService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ResendEmailConfirmationCommand request,
        CancellationToken cancellationToken)
    {
        string userId =
            request.UserId.Trim();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                userId);

        /*
         * نرجع Success في الحالات التالية:
         *
         * - المستخدم غير موجود.
         * - المستخدم محذوف.
         * - البريد مؤكد بالفعل.
         * - المستخدم ليس لديه بريد.
         *
         * الهدف هو عدم كشف معلومات عن الحسابات
         * الموجودة داخل النظام.
         */
        if (user is null ||
            user.IsDeleted ||
            user.EmailConfirmed ||
            string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Success();
        }

        /*
         * إنشاء OTP جديدة.
         *
         * خدمة Redis تتحقق أولًا من Resend Cooldown.
         *
         * بعد انتهاء الـ Cooldown:
         * - يتم حذف OTP القديمة.
         * - إنشاء OTP جديدة.
         * - إعادة TTL إلى 10 دقائق.
         * - إعادة attempts إلى صفر.
         */
        Result<EmailOtpCreated> otpResult =
            await _emailOtpService.CreateAsync(
                user.Id,
                user.Email,
                EmailOtpPurpose.ConfirmEmail,
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
            BuildEmailBody(
                otp.Code,
                expirationMinutes);

        try
        {
            await _emailSender.SendAsync(
                user.Email,
                "Your new NomoAI verification code",
                htmlBody,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            /*
             * لا نسجل OTP داخل Logs.
             *
             * OTP ستظل موجودة داخل Redis، ويمكن
             * للمستخدم طلب كود آخر بعد انتهاء
             * Resend Cooldown.
             */
            _logger.LogError(
                exception,
                "Failed to resend the email confirmation " +
                "OTP for user {UserId}.",
                user.Id);

            return Result.Failure(
                AuthErrors.EmailDeliveryFailed);
        }

        _logger.LogInformation(
            "Email confirmation OTP was resent successfully " +
            "for user {UserId}.",
            user.Id);

        return Result.Success();
    }

    private static string BuildEmailBody(
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
                        Confirm your email address
                    </h2>

                    <p style="
                        color:#374151;
                        line-height:1.7;">
                        You requested a new verification code.
                        Use the following code to confirm your
                        email address.
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
                        The previous verification code is no
                        longer valid.
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
                        If you did not request this code,
                        you can safely ignore this email.
                    </p>
                </div>
            </body>
            </html>
            """;
    }
}