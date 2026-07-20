using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Domain.Entities;
using System.Text;
using System.Text.Encodings.Web;

namespace NomoAI.API.Features.Auth.ConfirmEmailChange;

internal sealed class ConfirmEmailChangeHandler
    : IRequestHandler<
        ConfirmEmailChangeCommand,
        Result<ConfirmEmailChangeResponse>>
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly IEmailSender _emailSender;

    private readonly ILogger<ConfirmEmailChangeHandler>
        _logger;

    public ConfirmEmailChangeHandler(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ILogger<ConfirmEmailChangeHandler> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<ConfirmEmailChangeResponse>>
        Handle(
            ConfirmEmailChangeCommand request,
            CancellationToken cancellationToken)
    {
        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                request.UserId);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure<
                ConfirmEmailChangeResponse>(
                AuthErrors.UserNotFound);
        }

        string newEmail =
            request.NewEmail.Trim();

        /*
         * يجعل الضغط على زر التأكيد مرتين آمنًا.
         */
        if (string.Equals(
            user.Email,
            newEmail,
            StringComparison.OrdinalIgnoreCase))
        {
            return Result.Success(
                new ConfirmEmailChangeResponse(
                    newEmail,
                    "Email address has already been changed."));
        }

        ApplicationUser? existingUser =
            await _userManager.FindByEmailAsync(
                newEmail);

        if (existingUser is not null &&
            existingUser.Id != user.Id)
        {
            return Result.Failure<
                ConfirmEmailChangeResponse>(
                AuthErrors.EmailAlreadyInUse);
        }

        string decodedToken;

        try
        {
            decodedToken =
                Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(
                        request.Token));
        }
        catch (Exception exception)
            when (exception is FormatException
                or ArgumentException)
        {
            return Result.Failure<
                ConfirmEmailChangeResponse>(
                AuthErrors.InvalidEmailChangeToken);
        }

        string? oldEmail =
            user.Email;

        IdentityResult changeEmailResult =
            await _userManager.ChangeEmailAsync(
                user,
                newEmail,
                decodedToken);

        if (!changeEmailResult.Succeeded)
        {
            string description = string.Join(
                " | ",
                changeEmailResult.Errors
                    .Select(error =>
                        error.Description)
                    .Where(description =>
                        !string.IsNullOrWhiteSpace(
                            description))
                    .Distinct());

            if (string.IsNullOrWhiteSpace(
                description))
            {
                description =
                    "Email address could not be changed.";
            }

            return Result.Failure<
                ConfirmEmailChangeResponse>(
                AuthErrors.EmailChangeFailed(
                    description));
        }

        /*
         * التغيير أصبح محفوظًا بالفعل.
         * فشل رسائل الإشعار لا يجب أن يعكس التغيير.
         */
        if (!string.IsNullOrWhiteSpace(oldEmail))
        {
            string safeNewEmail =
                HtmlEncoder.Default.Encode(
                    newEmail);

            string oldEmailNotificationBody = $"""
                <div style="
                    font-family:Arial,sans-serif;
                    line-height:1.7;
                    color:#1f2937;">

                    <h2>Your email address was changed</h2>

                    <p>
                        The email address associated with your
                        NomoAI account has been changed.
                    </p>

                    <p>
                        New email address:
                        <strong>{safeNewEmail}</strong>
                    </p>

                    <p>
                        If you did not perform this action,
                        contact support and secure your account
                        immediately.
                    </p>
                </div>
                """;

            try
            {
                await _emailSender.SendAsync(
                    oldEmail,
                    "Your NomoAI email address was changed",
                    oldEmailNotificationBody,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to send completed email change notification to the old email for user {UserId}.",
                    user.Id);
            }
        }

        string newEmailNotificationBody = """
            <div style="
                font-family:Arial,sans-serif;
                line-height:1.7;
                color:#1f2937;">

                <h2>Email address confirmed</h2>

                <p>
                    This email address is now associated
                    with your NomoAI account.
                </p>

                <p>
                    You can now use it for future account
                    communication and sign-in where supported.
                </p>
            </div>
            """;

        try
        {
            await _emailSender.SendAsync(
                newEmail,
                "Your new email address is confirmed",
                newEmailNotificationBody,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to send completed email change notification to the new email for user {UserId}.",
                user.Id);
        }

        return Result.Success(
            new ConfirmEmailChangeResponse(
                newEmail,
                "Email address changed successfully."));
    }
}