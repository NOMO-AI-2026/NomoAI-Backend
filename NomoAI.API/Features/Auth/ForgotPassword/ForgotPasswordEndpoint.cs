using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ForgotPassword;

public static class ForgotPasswordEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/forgot-password",
                HandleAsync)
            .AllowAnonymous()
            .WithName("ForgotPassword")
            .WithSummary(
                "Request a password reset OTP")
            .WithDescription(
                "Creates and sends a one-time password " +
                "reset code when the account is eligible.")
            .Accepts<ForgotPasswordRequest>(
                "application/json")
            .Produces<ForgotPasswordResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        ForgotPasswordRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command =
            new ForgotPasswordCommand(
                request.Email);

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
            new ForgotPasswordResponse(
                "If an eligible account exists, " +
                "a password reset code has been sent."));
    }
}