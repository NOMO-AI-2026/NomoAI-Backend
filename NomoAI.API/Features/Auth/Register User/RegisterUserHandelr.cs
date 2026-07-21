using System.Text;
using System.Text.Encodings.Web;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Auth.Register_User
{
    public class RegisterUserHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDbContext dbContext,
        IEmailSender emailSender,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<RegisterUserHandler> logger)
        : IRequestHandler<
            RegisterUserCommand,
            Result<RegisterResponseDto>>
    {
        private const string ConfirmEmailPath =
            "/confirm-email";

        private readonly FrontendOptions _frontendOptions =
            frontendOptions.Value;

        public async Task<Result<RegisterResponseDto>> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken)
        {
            var email = request.Email.Trim();

            var existingUser =
                await userManager.FindByEmailAsync(email);

            if (existingUser is not null)
            {
                return Result.Failure<RegisterResponseDto>(
                    AuthErrors.UserAlreadyExists);
            }
            Console.WriteLine("Phone Number : " + request.PhoneNumber);
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Fullname = request.FullName,
                Age = request.Age,
                Gender = request.Gender,
                PhoneNumber = request.PhoneNumber
            };
            Console.WriteLine("Phone Number : " + request.PhoneNumber);

            var createResult =
                await userManager.CreateAsync(
                    user,
                    request.Password);

            if (!createResult.Succeeded)
            {
                return Result.Failure<RegisterResponseDto>(
                    AuthErrors.UserRegistrationFailed);
            }

            var roleName =
                request.Role == UserRole.Doctor
                    ? "Doctor"
                    : "Parent";

            var roleExists =
                await roleManager.RoleExistsAsync(roleName);

            if (!roleExists)
            {
                var createRoleResult =
                    await roleManager.CreateAsync(
                        new IdentityRole(roleName));

                if (!createRoleResult.Succeeded)
                {
                    return Result.Failure<RegisterResponseDto>(
                        AuthErrors.UserRegistrationFailed);
                }
            }

            var addToRoleResult =
                await userManager.AddToRoleAsync(
                    user,
                    roleName);

            if (!addToRoleResult.Succeeded)
            {
                return Result.Failure<RegisterResponseDto>(
                    AuthErrors.UserRegistrationFailed);
            }

            if (request.Role == UserRole.Doctor)
            {
                dbContext.Add(
                    new Doctor
                    {
                        UserId = user.Id
                    });
            }
            else
            {
                dbContext.Add(
                    new Parent
                    {
                        UserId = user.Id
                    });
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);

            /*
             * إنشاء وإرسال رابط تأكيد البريد.
             *
             * لو إرسال البريد فشل، يظل التسجيل ناجحاً؛
             * لأن الحساب تم إنشاؤه بالفعل، ويمكن للمستخدم
             * لاحقاً استخدام ResendEmailConfirmation.
             */
            try
            {
                await SendConfirmationEmailAsync(
                    user,
                    userManager,
                    emailSender,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                /*
                 * لا نسجل الـ Token أو رابط التأكيد
                 * حتى لا تظهر بيانات حساسة في Logs.
                 */
                logger.LogError(
                    exception,
                    "Failed to send the confirmation email for user {UserId}.",
                    user.Id);
            }

            return Result.Success(
                new RegisterResponseDto
                {
                    UserId = user.Id,
                    FullName = user.Fullname,
                    Username = user.Email
                });
        }

        private async Task SendConfirmationEmailAsync(
            ApplicationUser user,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new InvalidOperationException(
                    "The registered user does not have an email address.");
            }

          
            var rawToken =
                await userManager
                    .GenerateEmailConfirmationTokenAsync(user);

            
            var encodedToken =
                WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(rawToken));

            var confirmationLink =
                BuildConfirmationLink(
                    user.Id,
                    encodedToken);

         
            var safeConfirmationLink =
                HtmlEncoder.Default.Encode(
                    confirmationLink);

            const string subject =
                "Confirm your NomoAI email";

            var htmlBody = $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport"
                          content="width=device-width, initial-scale=1.0">
                </head>

                <body style="
                    margin: 0;
                    padding: 24px;
                    background-color: #f5f7fa;
                    font-family: Arial, sans-serif;">

                    <div style="
                        max-width: 600px;
                        margin: 0 auto;
                        padding: 32px;
                        background-color: #ffffff;
                        border: 1px solid #e5e7eb;
                        border-radius: 10px;">

                        <h2 style="
                            margin-top: 0;
                            color: #111827;">
                            Welcome to NomoAI
                        </h2>

                        <p style="
                            color: #374151;
                            line-height: 1.7;">
                            Please confirm your email address
                            by clicking the button below.
                        </p>

                        <div style="
                            margin: 30px 0;
                            text-align: center;">

                            <a href="{safeConfirmationLink}"
                               style="
                                   display: inline-block;
                                   padding: 13px 24px;
                                   background-color: #2563eb;
                                   color: #ffffff;
                                   text-decoration: none;
                                   border-radius: 7px;
                                   font-weight: bold;">
                                Confirm Email
                            </a>
                        </div>

                        <p style="
                            color: #6b7280;
                            font-size: 14px;
                            line-height: 1.6;">
                            If you did not create this account,
                            you can safely ignore this email.
                        </p>

                        <p style="
                            color: #6b7280;
                            font-size: 13px;
                            overflow-wrap: anywhere;">
                            If the button does not work,
                            copy and open this link:
                            <br>
                            {safeConfirmationLink}
                        </p>
                    </div>
                </body>
                </html>
                """;

            await emailSender.SendAsync(
                user.Email,
                subject,
                htmlBody,
                cancellationToken);
        }

        private string BuildConfirmationLink(
            string userId,
            string encodedToken)
        {
            var baseUrl =
                _frontendOptions.BaseUrl.TrimEnd('/');

            var safeUserId =
                Uri.EscapeDataString(userId);

            var safeToken =
                Uri.EscapeDataString(encodedToken);

            return
                $"{baseUrl}{ConfirmEmailPath}" +
                $"?userId={safeUserId}" +
                $"&token={safeToken}";
        }
    }
}