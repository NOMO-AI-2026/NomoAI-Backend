using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Features.Auth.Login_User;

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
                "Validates the refresh token, rotates it, and " +
                "returns a new access token together with a new " +
                "refresh token. Reused refresh tokens revoke the " +
                "entire family.")
            .Accepts<RefreshRequest>(
                "application/json")
            .Produces<Result<LoginResponseDto>>(
                StatusCodes.Status200OK)
            .Produces<Result<LoginResponseDto>>(
                StatusCodes.Status401Unauthorized)
            .Produces<Error>(
                StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        RefreshRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command =
            new RefreshCommand(
                request.RefreshToken);

        Result<LoginResponseDto> result =
            await sender.Send(
                command,
                cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToProblem();
    }
}
