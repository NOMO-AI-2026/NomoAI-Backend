using MediatR;
using NomoAI.API.Common.Abstractions;

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
                "Revokes the supplied refresh token. Missing or " +
                "invalid tokens still return success so logout " +
                "is idempotent.")
            .Accepts<LogoutRequest>(
                "application/json")
            .Produces<LogoutResponse>(
                StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        LogoutRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command =
            new LogoutCommand(
                request?.RefreshToken);

        await sender.Send(
            command,
            cancellationToken);

        return Results.Ok(
            new LogoutResponse(
                "Logged out successfully."));
    }
}
