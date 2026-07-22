using System.Net;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Email;
using NomoAI.API.Common.Enums;

namespace NomoAI.API.Infrastructure.Email;

public sealed class EmailTemplateBuilder : IEmailTemplateBuilder
{
    public EmailMessage BuildOtpMessage(
        EmailOtpPurpose purpose,
        string otp,
        int expirationMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(otp);

        string encodedOtp =
            WebUtility.HtmlEncode(otp);

        string encodedExpiration =
            WebUtility.HtmlEncode(
                expirationMinutes.ToString());

        return purpose switch
        {
            EmailOtpPurpose.ConfirmEmail =>
                BuildConfirmEmailMessage(
                    encodedOtp,
                    encodedExpiration),

            EmailOtpPurpose.ChangeEmail =>
                BuildChangeEmailMessage(
                    encodedOtp,
                    encodedExpiration),

            EmailOtpPurpose.ResetPassword =>
                BuildResetPasswordMessage(
                    encodedOtp,
                    encodedExpiration),

            _ => throw new ArgumentOutOfRangeException(
                nameof(purpose),
                purpose,
                $"Unsupported email OTP purpose: {purpose}.")
        };
    }

    public EmailMessage BuildEmailChangedNotification(
        string newEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newEmail);

        string encodedNewEmail =
            WebUtility.HtmlEncode(newEmail);

        string bodyContent = $"""
            <p style="
                color:#374151;
                line-height:1.7;">
                The email address associated with your
                NomoAI account has been changed to:
            </p>

            <p style="
                font-weight:bold;
                color:#111827;">
                {encodedNewEmail}
            </p>

            <p style="
                color:#6b7280;
                font-size:14px;
                line-height:1.6;">
                If you did not make this change,
                secure your account immediately.
            </p>
            """;

        return new EmailMessage(
            "Your NomoAI email address was changed",
            WrapInLayout(
                "Email address changed",
                bodyContent));
    }

    private static EmailMessage BuildConfirmEmailMessage(
        string encodedOtp,
        string encodedExpiration)
    {
        string bodyContent =
            BuildOtpBodyContent(
                introHtml: """
                    <p style="
                        color:#374151;
                        line-height:1.7;">
                        Use the following verification code
                        to confirm your email address.
                    </p>
                    """,
                encodedOtp: encodedOtp,
                encodedExpiration: encodedExpiration,
                footerHtml: """
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
                        If you did not create this account
                        or request this code, you can safely
                        ignore this email.
                    </p>
                    """);

        return new EmailMessage(
            "Your NomoAI verification code",
            WrapInLayout(
                "Confirm your email address",
                bodyContent));
    }

    private static EmailMessage BuildChangeEmailMessage(
        string encodedOtp,
        string encodedExpiration)
    {
        string bodyContent =
            BuildOtpBodyContent(
                introHtml: """
                    <p style="
                        color:#374151;
                        line-height:1.7;">
                        Use the following verification code
                        to confirm your new email address.
                    </p>
                    """,
                encodedOtp: encodedOtp,
                encodedExpiration: encodedExpiration,
                footerHtml: """
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
                    """);

        return new EmailMessage(
            "Confirm your new NomoAI email address",
            WrapInLayout(
                "Confirm your new email address",
                bodyContent));
    }

    private static EmailMessage BuildResetPasswordMessage(
        string encodedOtp,
        string encodedExpiration)
    {
        string bodyContent =
            BuildOtpBodyContent(
                introHtml: """
                    <p style="
                        color:#374151;
                        line-height:1.7;">
                        Use the following verification code
                        to reset your NomoAI password.
                    </p>
                    """,
                encodedOtp: encodedOtp,
                encodedExpiration: encodedExpiration,
                footerHtml: """
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
                    """);

        return new EmailMessage(
            "Your NomoAI password reset code",
            WrapInLayout(
                "Reset your password",
                bodyContent));
    }

    private static string BuildOtpBodyContent(
        string introHtml,
        string encodedOtp,
        string encodedExpiration,
        string footerHtml)
    {
        return $"""
            {introHtml}

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
                    {encodedOtp}
                </span>
            </div>

            <p style="
                color:#374151;
                line-height:1.7;">
                This code expires in approximately
                {encodedExpiration} minutes.
            </p>

            {footerHtml}
            """;
    }

    private static string WrapInLayout(
        string title,
        string bodyContent)
    {
        string encodedTitle =
            WebUtility.HtmlEncode(title);

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
                        {encodedTitle}
                    </h2>

                    {bodyContent}
                </div>
            </body>
            </html>
            """;
    }
}
