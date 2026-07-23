using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Email;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.Auth.ResendEmailConfirmation;

public sealed class ResendEmailConfirmationHandler
    : IRequestHandler<ResendEmailConfirmationCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailOtpDispatcher _emailOtpDispatcher;

    public ResendEmailConfirmationHandler(
        UserManager<ApplicationUser> userManager,
        IEmailOtpDispatcher emailOtpDispatcher)
    {
        _userManager = userManager;
        _emailOtpDispatcher = emailOtpDispatcher;
    }

    public async Task<Result> Handle(
        ResendEmailConfirmationCommand request,
        CancellationToken cancellationToken)
    {
        string userId =
            request.UserId.Trim();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(userId);

        // Generic success for ineligible accounts to avoid account enumeration.
        if (user is null ||
            user.IsDeleted ||
            user.EmailConfirmed ||
            string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Success();
        }

        Result<EmailOtpDispatchResult> dispatchResult =
            await _emailOtpDispatcher.SendAsync(
                user.Id,
                user.Email,
                EmailOtpPurpose.ConfirmEmail,
                cancellationToken);

        if (dispatchResult.IsFailure)
        {
            return Result.Failure(dispatchResult.Error);
        }

        return Result.Success();
    }
}
