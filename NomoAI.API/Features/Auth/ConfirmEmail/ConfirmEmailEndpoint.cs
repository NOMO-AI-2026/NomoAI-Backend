using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ConfirmEmail;

public static class ConfirmEmailEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/confirm-email",
                HandleAsync)
            .AllowAnonymous()
            .WithName("ConfirmEmail")
            .WithSummary(
                "Confirm a user's email using OTP")
            .WithDescription(
                "Validates the one-time verification code " +
                "sent to the user's email address.")
            .Accepts<ConfirmEmailRequest>(
                "application/json")
            .Produces<ConfirmEmailResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces<Error>(
                StatusCodes.Status409Conflict)
            .Produces<Error>(
                StatusCodes.Status429TooManyRequests)
            .Produces<Error>(
                StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleAsync(
        ConfirmEmailRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command =
            new ConfirmEmailCommand(
                request.UserId,
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
            new ConfirmEmailResponse(
                "Email confirmed successfully."));
    }
}