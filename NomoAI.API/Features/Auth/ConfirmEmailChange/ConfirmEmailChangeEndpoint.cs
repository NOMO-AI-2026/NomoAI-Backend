using System.Security.Claims;
using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ConfirmEmailChange;

public static class ConfirmEmailChangeEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/confirm-email-change",
                HandleAsync)
            .RequireAuthorization()
            .WithName("ConfirmEmailChange")
            .WithSummary(
                "Confirm changing the user's email using OTP")
            .WithDescription(
                "Validates the OTP sent to the new email " +
                "address and completes the email change.")
            .Accepts<ConfirmEmailChangeRequest>(
                "application/json")
            .Produces<ConfirmEmailChangeResponse>(
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
        ConfirmEmailChangeRequest request,
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
            new ConfirmEmailChangeCommand(
                userId,
                request.Otp);

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
            new ConfirmEmailChangeResponse(
                "Email address changed successfully."));
    }
}