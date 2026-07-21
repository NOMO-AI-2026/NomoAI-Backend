namespace NomoAI.API.Common.EmailOtp;

public sealed class EmailOtpOptions
{
    public const string SectionName =
        "EmailOtp";

    public int Length { get; init; } = 6;

    public int ExpirationMinutes { get; init; }
        = 10;

    public int MaxAttempts { get; init; }
        = 5;

    public int ResendCooldownSeconds { get; init; }
        = 60;

    public string HashKey { get; init; }
        = string.Empty;
}