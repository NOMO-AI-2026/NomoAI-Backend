using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Email;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ChangeEmail;

public sealed class ChangeEmailHandler
    : IRequestHandler<ChangeEmailCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailOtpDispatcher _emailOtpDispatcher;

    public ChangeEmailHandler(
        UserManager<ApplicationUser> userManager,
        IEmailOtpDispatcher emailOtpDispatcher)
    {
        _userManager = userManager;
        _emailOtpDispatcher = emailOtpDispatcher;
    }

    public async Task<Result> Handle(
        ChangeEmailCommand request,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user =
            await _userManager.FindByIdAsync(request.UserId);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(AuthErrors.UnauthorizedAccess);
        }

        bool passwordIsValid =
            await _userManager.CheckPasswordAsync(
                user,
                request.CurrentPassword);

        if (!passwordIsValid)
        {
            return Result.Failure(AuthErrors.IncorrectPassword);
        }

        string newEmail =
            request.NewEmail.Trim();

        string normalizedNewEmail =
            _userManager.NormalizeEmail(newEmail);

        if (string.Equals(
            user.NormalizedEmail,
            normalizedNewEmail,
            StringComparison.Ordinal))
        {
            return Result.Failure(AuthErrors.EmailUnchanged);
        }

        ApplicationUser? existingUser =
            await _userManager.FindByEmailAsync(newEmail);

        if (existingUser is not null &&
            existingUser.Id != user.Id)
        {
            return Result.Failure(AuthErrors.EmailAlreadyInUse);
        }

        Result<EmailOtpDispatchResult> dispatchResult =
            await _emailOtpDispatcher.SendAsync(
                user.Id,
                newEmail,
                EmailOtpPurpose.ChangeEmail,
                cancellationToken);

        if (dispatchResult.IsFailure)
        {
            return Result.Failure(dispatchResult.Error);
        }

        return Result.Success();
    }
}
