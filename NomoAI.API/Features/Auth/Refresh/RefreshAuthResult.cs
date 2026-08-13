namespace NomoAI.API.Features.Auth.Refresh;

public sealed record RefreshAuthResult(
    AccessTokenResponseDto Response,
    string RawRefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
