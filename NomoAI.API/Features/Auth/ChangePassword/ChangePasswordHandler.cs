using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Jwt;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ChangePassword;

internal sealed class ChangePasswordHandler
    : IRequestHandler<ChangePasswordCommand, Result<ChangePasswordResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenService refreshTokenService,
        ILogger<ChangePasswordHandler> logger)
    {
        _userManager = userManager;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<Result<ChangePasswordResponse>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user =
            await _userManager.FindByIdAsync(request.UserId);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure<ChangePasswordResponse>(
                AuthErrors.UserNotFound);
        }

        IdentityResult changePasswordResult =
            await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);

        if (!changePasswordResult.Succeeded)
        {
            string description =
                BuildFailureDescription(changePasswordResult);

            return Result.Failure<ChangePasswordResponse>(
                AuthErrors.ChangePasswordFailed(description));
        }

        await TryRevokeAllRefreshTokensAsync(
            user.Id,
            cancellationToken);

        return Result.Success(
            new ChangePasswordResponse(
                "Password changed successfully."));
    }

    private async Task TryRevokeAllRefreshTokensAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _refreshTokenService.RevokeAllForUserAsync(
                userId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Password was changed, but refresh tokens could not " +
                "be revoked for user {UserId}.",
                userId);
        }
    }

    private static string BuildFailureDescription(
        IdentityResult changePasswordResult)
    {
        string description = string.Join(
            " | ",
            changePasswordResult.Errors
                .Select(error => error.Description)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Distinct());

        if (string.IsNullOrWhiteSpace(description))
        {
            return "Password could not be changed.";
        }

        return description;
    }
}
