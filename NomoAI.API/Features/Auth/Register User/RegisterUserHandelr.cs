using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Email;
using NomoAI.API.Common.Enums;
using NomoAI.API.Common.Roles;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Auth.Register_User;


public sealed class RegisterUserHandler
    : IRequestHandler<RegisterUserCommand, Result<RegisterResponseDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _dbContext;
    private readonly IEmailOtpDispatcher _emailOtpDispatcher;
    private readonly IRoleManger _roleManger;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDbContext dbContext,
        IEmailOtpDispatcher emailOtpDispatcher,
        IRoleManger roleManger,
        ILogger<RegisterUserHandler> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _emailOtpDispatcher = emailOtpDispatcher;
        _roleManger = roleManger;
        _logger = logger;
    }


    public async Task<Result<RegisterResponseDto>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        string email =
            request.Email.Trim();

        ApplicationUser? existingUser =
            await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            if (!existingUser.IsDeleted)
            {
                return Result.Failure<RegisterResponseDto>(AuthErrors.UserAlreadyExists);
            }

            existingUser.IsDeleted = false;
            existingUser.Fullname = request.FullName;
            existingUser.Age = request.Age;
            existingUser.Gender = request.Gender;
            existingUser.PhoneNumber = request.PhoneNumber;

            await userManager.RemovePasswordAsync(existingUser);
            await userManager.AddPasswordAsync(existingUser, request.Password);
            await _roleManger.AddToRole(existingUser, request.Role);
            existingUser.EmailConfirmed = false;
            await userManager.UpdateAsync(existingUser);

            await TrySendConfirmationOtpAsync(
               existingUser,
               emailOtpService,
               emailSender,
               logger,
               cancellationToken);

            return Result.Success(
                new RegisterResponseDto
                {
                    UserId = existingUser.Id,
                    FullName = existingUser.Fullname,
                    Username = existingUser.Email ?? email
                });

        }
        else
        {

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Fullname = request.FullName,
                Age = request.Age,
                Gender = request.Gender,
                PhoneNumber = request.PhoneNumber
            };

            IdentityResult createResult =
                await userManager.CreateAsync(
                    user,
                    request.Password);

            if (!createResult.Succeeded)
            {
                return createRoleResult;
            }


           await _roleManger.AddToRole(
                user,
                request.Role);

            await dbContext.SaveChangesAsync(
                cancellationToken);


            await TrySendConfirmationOtpAsync(
                user,
                emailOtpService,
                emailSender,
                logger,
                cancellationToken);

            return Result.Success(
                new RegisterResponseDto
                {
                    UserId = user.Id,
                    FullName = user.Fullname,
                    Username = user.Email ?? email
                });
        }
    }

    private async Task TrySendConfirmationOtpAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning(
                "Confirmation OTP was not created because user {UserId} has no email address.",
                user.Id);

            return;
        }

        Result<EmailOtpDispatchResult> dispatchResult =
            await _emailOtpDispatcher.SendAsync(
                user.Id,
                user.Email,
                EmailOtpPurpose.ConfirmEmail,
                cancellationToken);

        if (dispatchResult.IsFailure)
        {
            _logger.LogError(
                "Failed to send confirmation OTP for user {UserId}. ErrorCode: {ErrorCode}.",
                user.Id,
                dispatchResult.Error.Code);
        }
    }
}
