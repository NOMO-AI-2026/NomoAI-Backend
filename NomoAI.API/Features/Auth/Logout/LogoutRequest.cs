namespace NomoAI.API.Features.Auth.Logout;

public sealed record LogoutRequest(
    string? RefreshToken);
