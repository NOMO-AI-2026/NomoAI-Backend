namespace NomoAI.API.Common.Jwt;

public static class RefreshTokenCookie
{
    public const string Name = "refreshToken";
    public const string Path = "/api/auth";

    public static string? Read(HttpRequest request)
    {
        return request.Cookies[Name];
    }

    public static void Append(
        HttpResponse response,
        IWebHostEnvironment environment,
        string rawToken,
        DateTime expiresAtUtc)
    {
        response.Cookies.Append(
            Name,
            rawToken,
            CreateOptions(environment, expiresAtUtc));
    }

    public static void Delete(
        HttpResponse response,
        IWebHostEnvironment environment)
    {
        response.Cookies.Delete(
            Name,
            CreateOptions(environment, DateTime.UnixEpoch));
    }

    private static CookieOptions CreateOptions(
        IWebHostEnvironment environment,
        DateTime expiresAtUtc)
    {
        bool isDevelopment = environment.IsDevelopment();

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment,
            SameSite = isDevelopment
                ? SameSiteMode.Lax
                : SameSiteMode.None,
            Path = Path,
            Expires = DateTime.SpecifyKind(
                expiresAtUtc,
                DateTimeKind.Utc),
            IsEssential = true
        };
    }
}
