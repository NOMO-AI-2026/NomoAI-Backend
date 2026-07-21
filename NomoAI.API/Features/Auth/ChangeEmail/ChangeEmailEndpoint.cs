using System.Security.Claims;
using MediatR;
using NomoAI.API.Common.Abstractions;

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
                "Verifies the current password and sends " +
                "an OTP to the new email address.")
            .Accepts<ChangeEmailRequest>(
                "application/json")
            .Produces<ChangeEmailResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces<Error>(
                StatusCodes.Status401Unauthorized)
            .Produces<Error>(
                StatusCodes.Status409Conflict)
            .Produces<Error>(
                StatusCodes.Status429TooManyRequests)
            .Produces<Error>(
                StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleAsync(
        ChangeEmailRequest request,
        ClaimsPrincipal claimsPrincipal,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId =
            claimsPrincipal.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var command =
            new ChangeEmailCommand(
                userId,
                request.CurrentPassword,
                request.NewEmail);

        Result result =
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

        return Results.Ok(
            new ChangeEmailResponse(
                "A verification code has been sent " +
                "to the new email address."));
    }
}