using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Email;
using NomoAI.API.Common.Enums;
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
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDbContext dbContext,
        IEmailOtpDispatcher emailOtpDispatcher,
        ILogger<RegisterUserHandler> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _emailOtpDispatcher = emailOtpDispatcher;
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

        if (existingUser is not null)
        {
            return Result.Failure<RegisterResponseDto>(
                AuthErrors.UserAlreadyExists);
        }

        ApplicationUser user =
            CreateUser(request, email);

        IdentityResult createResult =
            await _userManager.CreateAsync(
                user,
                request.Password);

        if (!createResult.Succeeded)
        {
            return Result.Failure<RegisterResponseDto>(
                AuthErrors.UserRegistrationFailed);
        }

        string roleName =
            GetRoleName(request.Role);

        IdentityResult ensureRoleResult =
            await EnsureUserRoleAsync(user, roleName);

        if (!ensureRoleResult.Succeeded)
        {
            return Result.Failure<RegisterResponseDto>(
                AuthErrors.UserRegistrationFailed);
        }

        AddProfile(user.Id, request.Role);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await TrySendConfirmationOtpAsync(
            user,
            cancellationToken);

        return Result.Success(
            new RegisterResponseDto
            {
                UserId = user.Id,
                FullName = user.Fullname,
                Username = user.Email
            });
    }

    private static ApplicationUser CreateUser(
        RegisterUserCommand request,
        string email)
    {
        return new ApplicationUser
        {
            UserName = email,
            Email = email,
            Fullname = request.FullName,
            Age = request.Age,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber
        };
    }

    private static string GetRoleName(UserRole role)
    {
        return role == UserRole.Doctor
            ? "Doctor"
            : "Parent";
    }

    private async Task<IdentityResult> EnsureUserRoleAsync(
        ApplicationUser user,
        string roleName)
    {
        bool roleExists =
            await _roleManager.RoleExistsAsync(roleName);

        if (!roleExists)
        {
            IdentityResult createRoleResult =
                await _roleManager.CreateAsync(
                    new IdentityRole(roleName));

            if (!createRoleResult.Succeeded)
            {
                return createRoleResult;
            }
        }

        return await _userManager.AddToRoleAsync(
            user,
            roleName);
    }

    private void AddProfile(
        string userId,
        UserRole role)
    {
        if (role == UserRole.Doctor)
        {
            _dbContext.Add(
                new Doctor
                {
                    UserId = userId
                });
        }
        else
        {
            _dbContext.Add(
                new Parent
                {
                    UserId = userId
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
