using MediatR;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Auth.ChangeEmail;

public static class ChangeEmailEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/change-email",
                HandleAsync)
            .RequireAuthorization()
            .WithName("ChangeEmail")
            .WithSummary(
                "Request an email address change")
            .WithDescription(
                "Validates the current password and sends " +
                "a confirmation link to the new email address.")
            .Accepts<ChangeEmailRequest>(
                "application/json")
            .Produces<ChangeEmailResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces<Error>(
                StatusCodes.Status404NotFound)
            .Produces<Error>(
                StatusCodes.Status409Conflict)
            .Produces<Error>(
                StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleAsync(
        ChangeEmailRequest request,
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

        var command = new ChangeEmailCommand(
            userId,
            request.NewEmail,
            request.CurrentPassword);

        Result<ChangeEmailResponse> result =
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