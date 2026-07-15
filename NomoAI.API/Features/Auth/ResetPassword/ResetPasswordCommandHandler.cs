using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandler: IRequestHandler<ResetPasswordCommand, Result>
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly ILogger<ResetPasswordCommandHandler> _logger;
	public ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager,ILogger<ResetPasswordCommandHandler> logger)
	{
		_userManager = userManager;
		_logger = logger;
	}

	public async Task<Result> Handle(ResetPasswordCommand request,CancellationToken cancellationToken)
	{
		var user = await _userManager.FindByIdAsync(request.UserId);

		/*
         * We return InvalidToken instead of UserNotFound.
         *
         * From the client's perspective, a missing user and an invalid
         * reset token both mean that the reset link cannot be used.
         */
		if (user is null)
		{
			return Result.Failure(AuthErrors.InvalidToken);
		}

		string decodedToken;

		try
		{
			var tokenBytes =WebEncoders.Base64UrlDecode(request.Token);

			decodedToken =Encoding.UTF8.GetString(tokenBytes);
		}
		catch (FormatException exception)
		{
			_logger.LogWarning(
				exception,
				"Password reset token has an invalid format for user {UserId}.",
				request.UserId);

			return Result.Failure(AuthErrors.InvalidToken);
		}
		catch (ArgumentException exception)
		{
			_logger.LogWarning(
				exception,
				"Password reset token could not be decoded for user {UserId}.",
				request.UserId);

			return Result.Failure(AuthErrors.InvalidToken);
		}

		var resetPasswordResult =await _userManager.ResetPasswordAsync(user,decodedToken,request.NewPassword);

		if (resetPasswordResult.Succeeded)
		{
			_logger.LogInformation("Password was reset successfully for user {UserId}.",user.Id);

			return Result.Success();
		}

		var identityErrors =resetPasswordResult.Errors.ToArray();

		_logger.LogWarning(
			"Password reset failed for user {UserId}. Errors: {Errors}",
			user.Id,
			string.Join(
				", ",
				identityErrors.Select(error =>
					$"{error.Code}: {error.Description}")));

		var containsInvalidTokenError =
			identityErrors.Any(error =>
				string.Equals(
					error.Code,
					"InvalidToken",
					StringComparison.OrdinalIgnoreCase));

		if (containsInvalidTokenError)
		{
			return Result.Failure(AuthErrors.InvalidToken);
		}

		/*
         * These are normally password-policy errors, such as:
         *
         * - PasswordTooShort
         * - PasswordRequiresDigit
         * - PasswordRequiresUpper
         * - PasswordRequiresLower
         * - PasswordRequiresNonAlphanumeric
         */
		var errorDescription = string.Join(
			" | ",
			identityErrors.Select(error => error.Description));

		return Result.Failure(
			AuthErrors.PasswordResetFailed(errorDescription));
	}
}