using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ResetPassword;

public static class ResetPasswordEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/reset-password",
                HandleAsync)
            .AllowAnonymous()
            .WithName("ResetPassword")
            .WithSummary(
                "Reset the user's password using OTP")
            .WithDescription(
                "Validates the password reset OTP and " +
                "sets a new password for the user.")
            .Accepts<ResetPasswordRequest>(
                "application/json")
            .Produces<ResetPasswordResponse>(
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
        ResetPasswordRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command =
            new ResetPasswordCommand(
                request.Email,
                request.Otp,
                request.NewPassword,
                request.ConfirmNewPassword);

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
            new ResetPasswordResponse(
                "Your password has been reset successfully."));
    }
}