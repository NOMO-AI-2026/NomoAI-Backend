namespace NomoAI.API.Common.Email;

public sealed record EmailOtpDispatchResult(
    DateTime ExpiresAtUtc,
    DateTime ResendAvailableAtUtc);
