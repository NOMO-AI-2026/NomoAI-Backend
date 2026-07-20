using MediatR;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Auth.ChangePassword;

public static class ChangePasswordEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/change-password",
                HandleAsync)
            .RequireAuthorization()
            .WithName("ChangePassword")
            .WithSummary(
                "Change the authenticated user's password")
            .WithDescription(
                "Changes the authenticated user's password " +
                "after validating the current password.")
            .Accepts<ChangePasswordRequest>(
                "application/json")
            .Produces<ChangePasswordResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces<Error>(
                StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId =
            currentUser.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var command =
            new ChangePasswordCommand(
                userId,
                request.CurrentPassword,
                request.NewPassword,
                request.ConfirmNewPassword);

        Result<ChangePasswordResponse> result =
            await sender.Send(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(
                result.Error,
                statusCode:
                    result.Error.StatusCode);
        }

        return Results.Ok(result.Value);
    }
}