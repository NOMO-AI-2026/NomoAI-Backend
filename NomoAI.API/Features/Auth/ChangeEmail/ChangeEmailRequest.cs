namespace NomoAI.API.Features.Auth.ChangeEmail;

public sealed record ChangeEmailRequest(
    string NewEmail,
    string CurrentPassword);