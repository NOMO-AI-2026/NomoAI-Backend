using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Jwt;

namespace NomoAI.API.Features.Auth.Refresh;

public static class RefreshEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/refresh",
                HandleAsync)
            .AllowAnonymous()
            .WithName("Refresh")
            .WithSummary(
                "Rotate a refresh token and issue a new access token")
            .WithDescription(
                "Reads the HttpOnly refresh-token cookie, rotates " +
                "it, and returns a new access token. Reused " +
                "refresh tokens revoke the entire family.")
            .Produces<AccessTokenResponseDto>(
                StatusCodes.Status200OK)
            .Produces<Result<AccessTokenResponseDto>>(
                StatusCodes.Status401Unauthorized)
            .Produces<Error>(
                StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        ISender sender,
        HttpContext httpContext,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        string? refreshToken = RefreshTokenCookie.Read(httpContext.Request);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Failure<AccessTokenResponseDto>(
                AuthErrors.InvalidRefreshToken).ToProblem();
        }

        Result<RefreshAuthResult> result =
            await sender.Send(
                new RefreshCommand(refreshToken),
                cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        RefreshAuthResult authResult = result.Value;

        RefreshTokenCookie.Append(
            httpContext.Response,
            environment,
            authResult.RawRefreshToken,
            authResult.RefreshTokenExpiresAtUtc);

        return Results.Ok(authResult.Response);
    }
}
