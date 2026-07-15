using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Domain.Entities;
using System.Text;
using System.Text.Encodings.Web;

namespace NomoAI.API.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandHandler
	: IRequestHandler<ForgotPasswordCommand, Result>
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly IEmailSender _emailSender;
	private readonly FrontendOptions _frontendOptions;
	private readonly ILogger<ForgotPasswordCommandHandler> _logger;

	public ForgotPasswordCommandHandler(
		UserManager<ApplicationUser> userManager,
		IEmailSender emailSender,
		IOptions<FrontendOptions> frontendOptions,
		ILogger<ForgotPasswordCommandHandler> logger)
	{
		_userManager = userManager;
		_emailSender = emailSender;
		_frontendOptions = frontendOptions.Value;
		_logger = logger;
	}

	public async Task<Result> Handle(
		ForgotPasswordCommand request,
		CancellationToken cancellationToken)
	{
		var email = request.Email.Trim();

		var user = await _userManager.FindByEmailAsync(email);

		/*
         * Do not return AuthErrors.UserNotFound here.
         *
         * Returning UserNotFound would allow attackers to discover
         * which email addresses are registered in the application.
         */
		if (user is null)
		{
			return Result.Success();
		}

		var passwordResetToken =
			await _userManager.GeneratePasswordResetTokenAsync(user);

		/*
         * Identity tokens may contain URL-unsafe characters.
         * Therefore, we encode the token before putting it in the URL.
         */
		var encodedToken = WebEncoders.Base64UrlEncode(
			Encoding.UTF8.GetBytes(passwordResetToken));

		var resetPasswordUrl = QueryHelpers.AddQueryString(
			_frontendOptions.ResetPasswordUrl,
			new Dictionary<string, string?>
			{
				["userId"] = user.Id,
				["token"] = encodedToken
			});

		var safeResetPasswordUrl =
			HtmlEncoder.Default.Encode(resetPasswordUrl);

		var emailBody = $"""
            <div style="font-family: Arial, sans-serif;">
                <h2>Reset your NomoAI password</h2>

                <p>
                    We received a request to reset your password.
                </p>

                <p>
                    Click the button below to create a new password.
                </p>

                <p>
                    <a href="{safeResetPasswordUrl}"
                       style="
                           display: inline-block;
                           padding: 12px 20px;
                           background-color: #2563eb;
                           color: white;
                           text-decoration: none;
                           border-radius: 6px;">
                        Reset Password
                    </a>
                </p>

                <p>
                    If you did not request this password reset,
                    you can safely ignore this email.
                </p>
            </div>
            """;

		try
		{
			await _emailSender.SendAsync(
				user.Email ?? email,
				"Reset your NomoAI password",
				emailBody,
				cancellationToken);
		}
		catch (Exception exception)
		{
			/*
             * We log the real failure internally.
             *
             * We still return the same public result because returning
             * a different response could reveal that the email exists.
             */
			_logger.LogError(
				exception,
				"Failed to send password reset email for user {UserId}.",
				user.Id);
		}

		return Result.Success();
	}
}