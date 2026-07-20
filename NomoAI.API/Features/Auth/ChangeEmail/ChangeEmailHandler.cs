using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Domain.Entities;
using System.Text;
using System.Text.Encodings.Web;

namespace NomoAI.API.Features.Auth.ChangeEmail;

internal sealed class ChangeEmailHandler
    : IRequestHandler<
        ChangeEmailCommand,
        Result<ChangeEmailResponse>>
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly IEmailSender _emailSender;

    private readonly FrontendOptions
        _frontendOptions;

    private readonly ILogger<ChangeEmailHandler>
        _logger;

    public ChangeEmailHandler(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<ChangeEmailHandler> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _frontendOptions = frontendOptions.Value;
        _logger = logger;
    }

    public async Task<Result<ChangeEmailResponse>> Handle(
        ChangeEmailCommand request,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                request.UserId);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure<ChangeEmailResponse>(
                AuthErrors.UserNotFound);
        }

        string newEmail = request.NewEmail.Trim();

        if (string.Equals(
            user.Email,
            newEmail,
            StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ChangeEmailResponse>(
                AuthErrors.EmailUnchanged);
        }

        bool passwordIsCorrect =
            await _userManager.CheckPasswordAsync(
                user,
                request.CurrentPassword);

        if (!passwordIsCorrect)
        {
            return Result.Failure<ChangeEmailResponse>(
                AuthErrors.IncorrectPassword);
        }

        ApplicationUser? existingUser =
            await _userManager.FindByEmailAsync(
                newEmail);

        if (existingUser is not null &&
            existingUser.Id != user.Id)
        {
            return Result.Failure<ChangeEmailResponse>(
                AuthErrors.EmailAlreadyInUse);
        }

        if (string.IsNullOrWhiteSpace(
            _frontendOptions
                .ConfirmEmailChangePageUrl))
        {
            _logger.LogError(
                "ConfirmEmailChangePageUrl is not configured.");

            return Result.Failure<ChangeEmailResponse>(
                AuthErrors.EmailDeliveryFailed);
        }

        string rawToken =
            await _userManager
                .GenerateChangeEmailTokenAsync(
                    user,
                    newEmail);

        string encodedToken =
            WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(rawToken));

        string confirmationPageUrl =
            QueryHelpers.AddQueryString(
                _frontendOptions
                    .ConfirmEmailChangePageUrl,
                new Dictionary<string, string?>
                {
                    ["userId"] = user.Id,
                    ["newEmail"] = newEmail,
                    ["token"] = encodedToken
                });

        string safeConfirmationUrl =
            HtmlEncoder.Default.Encode(
                confirmationPageUrl);

        string confirmationEmailBody = $"""
            <div style="
                font-family:Arial,sans-serif;
                line-height:1.7;
                color:#1f2937;">

                <h2>Confirm your new email address</h2>

                <p>
                    We received a request to use this email
                    address for your NomoAI account.
                </p>

                <p>
                    Click the button below, then confirm the
                    change from the confirmation page.
                </p>

                <p style="margin:24px 0;">
                    <a href="{safeConfirmationUrl}"
                       style="
                           display:inline-block;
                           padding:12px 20px;
                           background:#2563eb;
                           color:#ffffff;
                           text-decoration:none;
                           border-radius:6px;">
                        Review email change
                    </a>
                </p>

                <p>
                    Opening this link alone will not change
                    the email address. You must press the
                    confirmation button on the next page.
                </p>

                <p>
                    If you did not request this change,
                    ignore this email.
                </p>
            </div>
            """;

        try
        {
            await _emailSender.SendAsync(
                newEmail,
                "Confirm your new email address",
                confirmationEmailBody,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to send email change confirmation for user {UserId}.",
                user.Id);

            return Result.Failure<ChangeEmailResponse>(
                AuthErrors.EmailDeliveryFailed);
        }

        /*
         * إرسال الإشعار إلى البريد القديم عملية إضافية.
         * فشلها لا يلغي إرسال رابط التأكيد إلى البريد الجديد.
         */
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            string maskedNewEmail =
                HtmlEncoder.Default.Encode(
                    MaskEmail(newEmail));

            string oldEmailNotificationBody = $"""
                <div style="
                    font-family:Arial,sans-serif;
                    line-height:1.7;
                    color:#1f2937;">

                    <h2>Email change requested</h2>

                    <p>
                        A request was made to change the email
                        address associated with your NomoAI account.
                    </p>

                    <p>
                        Requested new email:
                        <strong>{maskedNewEmail}</strong>
                    </p>

                    <p>
                        Your current email is still active.
                        The change will not be completed until
                        the new email address is confirmed.
                    </p>

                    <p>
                        If you did not make this request,
                        change your password immediately.
                    </p>
                </div>
                """;

            try
            {
                await _emailSender.SendAsync(
                    user.Email,
                    "Email change requested",
                    oldEmailNotificationBody,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to notify the old email for user {UserId}.",
                    user.Id);
            }
        }

        return Result.Success(
            new ChangeEmailResponse(
                "A confirmation link has been sent to the new email address."));
    }

    private static string MaskEmail(string email)
    {
        int separatorIndex =
            email.IndexOf('@');

        if (separatorIndex <= 0)
        {
            return "***";
        }

        string localPart =
            email[..separatorIndex];

        string domain =
            email[separatorIndex..];

        if (localPart.Length == 1)
        {
            return $"{localPart[0]}***{domain}";
        }

        return
            $"{localPart[0]}***{localPart[^1]}{domain}";
    }
}