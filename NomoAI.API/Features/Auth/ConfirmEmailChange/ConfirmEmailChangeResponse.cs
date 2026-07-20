namespace NomoAI.API.Features.Auth.ConfirmEmailChange;

public sealed record ConfirmEmailChangeResponse(
    string Email,
    string Message);