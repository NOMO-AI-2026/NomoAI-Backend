using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Enums;

namespace NomoAI.API.Common.Abstractions.Email;

public interface IEmailOtpService
{
    Task<Result<EmailOtpCreated>> CreateAsync(
        string userId,
        string targetEmail,
        EmailOtpPurpose purpose,
        CancellationToken cancellationToken = default);

    Task<Result<VerifiedEmailOtp>> VerifyAndReserveAsync(
        string userId,
        string otp,
        EmailOtpPurpose purpose,
        CancellationToken cancellationToken = default);

    Task ConsumeAsync(
        string userId,
        EmailOtpPurpose purpose,
        string reservationToken,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        string userId,
        EmailOtpPurpose purpose,
        string reservationToken,
        CancellationToken cancellationToken = default);
}