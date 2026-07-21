namespace NomoAI.API.Features.Auth.ChangeEmail;

public sealed record ChangeEmailRequest(
    string CurrentPassword,
    string NewEmail);