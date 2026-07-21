using NomoAI.API.Common.Enums;

namespace NomoAI.API.Common.EmailOtp;

public sealed record EmailOtpCreated(
    string Code,
    DateTime ExpiresAtUtc,
    DateTime ResendAvailableAtUtc);

public sealed record VerifiedEmailOtp(
    string UserId,
    string TargetEmail,
    EmailOtpPurpose Purpose,
    string ReservationToken);