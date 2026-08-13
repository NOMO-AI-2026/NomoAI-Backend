namespace NomoAI.API.Features.Auth.Refresh;

public sealed record AccessTokenResponseDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt);
