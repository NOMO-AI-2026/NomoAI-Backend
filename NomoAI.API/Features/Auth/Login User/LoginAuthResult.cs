namespace NomoAI.API.Features.Auth.Login_User;

public sealed record LoginAuthResult(
    LoginResponseDto Response,
    string RawRefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
