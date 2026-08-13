using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Jwt;

namespace NomoAI.API.Features.Auth.Logout;

public static class LogoutEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/logout",
                HandleAsync)
            .AllowAnonymous()
            .WithName("Logout")
            .WithSummary(
                "Revoke a refresh token")
            .WithDescription(
                "Revokes the HttpOnly refresh-token cookie when " +
                "present and always expires the cookie. Missing " +
                "or invalid tokens still return success so logout " +
                "is idempotent.")
            .Produces<LogoutResponse>(
                StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        ISender sender,
        HttpContext httpContext,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        string? refreshToken = RefreshTokenCookie.Read(httpContext.Request);

        await sender.Send(
            new LogoutCommand(refreshToken),
            cancellationToken);

        RefreshTokenCookie.Delete(
            httpContext.Response,
            environment);

        return Results.Ok(
            new LogoutResponse(
                "Logged out successfully."));
    }
}
