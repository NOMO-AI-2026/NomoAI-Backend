using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ResendEmailConfirmation;

public static class ResendEmailConfirmationEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/resend-email-confirmation",
                HandleAsync)
            .AllowAnonymous()
            .WithName("ResendEmailConfirmation")
            .WithSummary(
                "Resend the email confirmation OTP")
            .WithDescription(
                "Creates and sends a new email verification " +
                "code when the resend cooldown has expired.")
            .Accepts<ResendEmailConfirmationRequest>(
                "application/json")
            .Produces<ResendEmailConfirmationResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces<Error>(
                StatusCodes.Status429TooManyRequests)
            .Produces<Error>(
                StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleAsync(
        ResendEmailConfirmationRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command =
            new ResendEmailConfirmationCommand(
                request.UserId);

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
            new ResendEmailConfirmationResponse(
                "If the account is eligible, a new " +
                "verification code has been sent."));
    }
}