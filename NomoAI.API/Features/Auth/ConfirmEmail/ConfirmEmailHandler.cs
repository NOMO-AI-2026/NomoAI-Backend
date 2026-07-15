using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ConfirmEmail;

public sealed class ConfirmEmailHandler : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ConfirmEmailHandler> _logger;

    public ConfirmEmailHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<ConfirmEmailHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);

        /*
         * Return InvalidToken instead of UserNotFound.
         *
         * From the client's perspective, an invalid user ID and an invalid
         * email confirmation token both mean that the confirmation link cannot be used.
         */
        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidToken);
        }

        /*
         * If the email is already confirmed, return success immediately.
         * This makes the endpoint idempotent.
         */
        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        string decodedToken;

        try
        {
            /*
             * The token is expected to have been encoded by the client using Base64UrlEncode.
             * Reverse this operation to get the original Identity token.
             */
            var tokenBytes = WebEncoders.Base64UrlDecode(request.Token);

            decodedToken = Encoding.UTF8.GetString(tokenBytes);
        }
        catch (FormatException exception)
        {
            _logger.LogWarning(
                exception,
                "Email confirmation token has an invalid format for user {UserId}.",
                request.UserId);

            return Result.Failure(AuthErrors.InvalidToken);
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(
                exception,
                "Email confirmation token could not be decoded for user {UserId}.",
                request.UserId);

            return Result.Failure(AuthErrors.InvalidToken);
        }

        var confirmEmailResult = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (confirmEmailResult.Succeeded)
        {
            _logger.LogInformation("Email was confirmed successfully for user {UserId}.", user.Id);

            return Result.Success();
        }

        var identityErrors = confirmEmailResult.Errors.ToArray();

        _logger.LogWarning(
            "Email confirmation failed for user {UserId}. Errors: {Errors}",
            user.Id,
            string.Join(
                ", ",
                identityErrors.Select(error =>
                    $"{error.Code}: {error.Description}")));

        /*
         * Return InvalidToken for any confirmation failure.
         * This includes token expiration and invalid tokens.
         */
        return Result.Failure(AuthErrors.InvalidToken);
    }
}