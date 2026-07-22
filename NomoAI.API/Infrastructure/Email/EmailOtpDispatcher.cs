using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Email;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;
using NomoAI.API.Features.Auth;

namespace NomoAI.API.Infrastructure.Email;

public sealed class EmailOtpDispatcher : IEmailOtpDispatcher
{
    private readonly IEmailOtpService _emailOtpService;
    private readonly IEmailTemplateBuilder _emailTemplateBuilder;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailOtpDispatcher> _logger;

    public EmailOtpDispatcher(
        IEmailOtpService emailOtpService,
        IEmailTemplateBuilder emailTemplateBuilder,
        IEmailSender emailSender,
        ILogger<EmailOtpDispatcher> logger)
    {
        _emailOtpService = emailOtpService;
        _emailTemplateBuilder = emailTemplateBuilder;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<EmailOtpDispatchResult>> SendAsync(
        string userId,
        string targetEmail,
        EmailOtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        Result<EmailOtpCreated> otpResult =
            await _emailOtpService.CreateAsync(
                userId,
                targetEmail,
                purpose,
                cancellationToken);

        if (otpResult.IsFailure)
        {
            return Result.Failure<EmailOtpDispatchResult>(
                otpResult.Error);
        }

        EmailOtpCreated otp =
            otpResult.Value;

        int expirationMinutes =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (otp.ExpiresAtUtc - DateTime.UtcNow)
                    .TotalMinutes));

        EmailMessage message =
            _emailTemplateBuilder.BuildOtpMessage(
                purpose,
                otp.Code,
                expirationMinutes);

        try
        {
            await _emailSender.SendAsync(
                targetEmail,
                message.Subject,
                message.HtmlBody,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to send OTP email for user {UserId} " +
                "with purpose {Purpose}.",
                userId,
                purpose);

            return Result.Failure<EmailOtpDispatchResult>(
                AuthErrors.EmailDeliveryFailed);
        }

        _logger.LogInformation(
            "OTP email was sent successfully for user {UserId} " +
            "with purpose {Purpose}.",
            userId,
            purpose);

        return Result.Success(
            new EmailOtpDispatchResult(
                otp.ExpiresAtUtc,
                otp.ResendAvailableAtUtc));
    }
}
