using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Common.EmailOtp;

public static class EmailOtpErrors
{
    public static readonly Error InvalidOrExpired = new(
        "EmailOtp.InvalidOrExpired",
        "The verification code is invalid or expired.",
        StatusCodes.Status400BadRequest);

    public static readonly Error AttemptsExceeded = new(
        "EmailOtp.AttemptsExceeded",
        "The maximum number of verification attempts has been exceeded.",
        StatusCodes.Status429TooManyRequests);

    public static readonly Error ResendTooSoon = new(
        "EmailOtp.ResendTooSoon",
        "Please wait before requesting another verification code.",
        StatusCodes.Status429TooManyRequests);

    public static readonly Error VerificationInProgress = new(
        "EmailOtp.VerificationInProgress",
        "The verification code is currently being processed.",
        StatusCodes.Status409Conflict);

    public static readonly Error ServiceUnavailable = new(
        "EmailOtp.ServiceUnavailable",
        "The verification service is temporarily unavailable. Please try again later.",
        StatusCodes.Status503ServiceUnavailable);
}