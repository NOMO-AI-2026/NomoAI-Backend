using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ChangePassword;

internal sealed class ChangePasswordHandler
    : IRequestHandler<ChangePasswordCommand, Result<ChangePasswordResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ChangePasswordHandler(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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

        return Result.Success(
            new ChangePasswordResponse(
                "Password changed successfully."));
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
