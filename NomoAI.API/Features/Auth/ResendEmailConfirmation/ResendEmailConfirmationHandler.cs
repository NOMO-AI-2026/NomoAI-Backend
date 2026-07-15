using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;
using System.Text;
using System.Text.Encodings.Web;

namespace NomoAI.API.Features.Auth.ResendEmailConfirmation;

public sealed class ResendEmailConfirmationHandler : IRequestHandler<ResendEmailConfirmationCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<ResendEmailConfirmationHandler> _logger;

    public ResendEmailConfirmationHandler(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        IEmailSender emailSender,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<ResendEmailConfirmationHandler> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _emailSender = emailSender;
        _frontendOptions = frontendOptions.Value;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ResendEmailConfirmationCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        /*
		 * Normalize the email using Identity's normalization to match
		 * how Identity stores and searches for emails.
		 */
        var normalizedEmail = _userManager.NormalizeEmail(email);

        var user = await _dbContext.ApplicationUsers
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted, cancellationToken);

       
        if (user is null)
        {
            return Result.Success();
        }

        /*
		 * If the email is already confirmed, return success without sending.
		 * This is still idempotent but prevents unnecessary email sends.
		 */
        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        /*
		 * If the user's stored email is null or invalid, return success.
		 */
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Success();
        }

        /*
		 * Generate a new email confirmation token.
		 */
        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        /*
		 * Identity tokens may contain URL-unsafe characters.
		 * Therefore, we encode the token before putting it in the URL.
		 */
        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(confirmationToken));

        var confirmEmailUrl = QueryHelpers.AddQueryString(
            _frontendOptions.ConfirmEmailUrl,
            new Dictionary<string, string?>
            {
                ["userId"] = user.Id,
                ["token"] = encodedToken
            });

        var safeConfirmEmailUrl = HtmlEncoder.Default.Encode(confirmEmailUrl);

        var emailBody = $"""
            <div style="font-family: Arial, sans-serif;">
                <h2>Confirm your NomoAI email</h2>

                <p>
                    Thank you for signing up. Please confirm your email address to activate your account.
                </p>

                <p>
                    Click the button below to confirm your email.
                </p>

                <p>
                    <a href="{safeConfirmEmailUrl}"
                       style="
                           display: inline-block;
                           padding: 12px 20px;
                           background-color: #2563eb;
                           color: white;
                           text-decoration: none;
                           border-radius: 6px;">
                        Confirm Email
                    </a>
                </p>

                <p>
                    If you did not create an account, you can safely ignore this email.
                </p>
            </div>
            """;

        try
        {
            await _emailSender.SendAsync(
                user.Email,
                "Confirm your NomoAI email",
                emailBody,
                cancellationToken);
        }
        catch (Exception exception)
        {
            
            _logger.LogError(
                exception,
                "Failed to send email confirmation email for user {UserId}.",
                user.Id);
        }

        return Result.Success();
    }
}