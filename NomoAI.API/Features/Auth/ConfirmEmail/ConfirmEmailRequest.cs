namespace NomoAI.API.Features.Auth.ConfirmEmail;

public sealed record ConfirmEmailRequest(
    string UserId,
    string Otp);