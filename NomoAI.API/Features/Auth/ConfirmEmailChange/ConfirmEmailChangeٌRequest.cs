namespace NomoAI.API.Features.Auth.ConfirmEmailChange;

public sealed record ConfirmEmailChangeRequest(
    string UserId,
    string NewEmail,
    string Token);