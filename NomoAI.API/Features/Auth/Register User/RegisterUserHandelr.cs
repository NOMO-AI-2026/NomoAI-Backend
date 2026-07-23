using MediatR;
using Microsoft.AspNetCore.Identity;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;
using NomoAI.API.Common.Roles;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Auth.Register_User;

public sealed class RegisterUserHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    AppDbContext dbContext,
    IEmailSender emailSender,
    IRoleManger _roleManger,
    IEmailOtpService emailOtpService,
    ILogger<RegisterUserHandler> logger)
    : IRequestHandler<
        RegisterUserCommand,
        Result<RegisterResponseDto>>
{
    public async Task<Result<RegisterResponseDto>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        string email =
            request.Email.Trim();

        ApplicationUser? existingUser =
            await userManager.FindByEmailAsync(email);

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
                return Result.Failure<RegisterResponseDto>(
                    AuthErrors.UserRegistrationFailed);
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

    private static async Task TrySendConfirmationOtpAsync(
        ApplicationUser user,
        IEmailOtpService emailOtpService,
        IEmailSender emailSender,
        ILogger<RegisterUserHandler> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning(
                "Confirmation OTP was not created because user {UserId} has no email address.",
                user.Id);

            return;
        }

        Result<EmailOtpCreated> otpResult =
            await emailOtpService.CreateAsync(
                user.Id,
                user.Email,
                EmailOtpPurpose.ConfirmEmail,
                cancellationToken);

        if (otpResult.IsFailure)
        {
            logger.LogError(
                "Failed to create confirmation OTP for user {UserId}. ErrorCode: {ErrorCode}.",
                user.Id,
                otpResult.Error.Code);

            return;
        }

        EmailOtpCreated otp =
            otpResult.Value;

        int expirationMinutes =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (otp.ExpiresAtUtc - DateTime.UtcNow)
                    .TotalMinutes));

        const string subject =
            "Your NomoAI verification code";

        string htmlBody = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta
                    name="viewport"
                    content="width=device-width, initial-scale=1.0">
            </head>

            <body style="
                margin:0;
                padding:24px;
                background-color:#f5f7fa;
                font-family:Arial,sans-serif;">

                <div style="
                    max-width:600px;
                    margin:0 auto;
                    padding:32px;
                    background-color:#ffffff;
                    border:1px solid #e5e7eb;
                    border-radius:10px;">

                    <h2 style="
                        margin-top:0;
                        color:#111827;">
                        Welcome to NomoAI
                    </h2>

                    <p style="
                        color:#374151;
                        line-height:1.7;">
                        Use the following verification code
                        to confirm your email address.
                    </p>

                    <div style="
                        margin:30px 0;
                        padding:20px;
                        background-color:#f3f4f6;
                        border-radius:8px;
                        text-align:center;">

                        <span style="
                            font-size:32px;
                            font-weight:bold;
                            letter-spacing:10px;
                            color:#111827;">
                            {otp.Code}
                        </span>
                    </div>

                    <p style="
                        color:#374151;
                        line-height:1.7;">
                        This code expires in approximately
                        {expirationMinutes} minutes.
                    </p>

                    <p style="
                        color:#6b7280;
                        font-size:14px;
                        line-height:1.6;">
                        Never share this code with anyone.
                    </p>

                    <p style="
                        color:#6b7280;
                        font-size:14px;
                        line-height:1.6;">
                        If you did not create this account,
                        you can safely ignore this email.
                    </p>
                </div>
            </body>
            </html>
            """;

        try
        {
            await emailSender.SendAsync(
                user.Email,
                subject,
                htmlBody,
                cancellationToken);

            logger.LogInformation(
                "Confirmation OTP email was sent successfully for user {UserId}.",
                user.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to send confirmation OTP email for user {UserId}.",
                user.Id);
        }
    }
}