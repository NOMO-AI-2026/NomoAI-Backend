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
            .AllowAnonymous()
            .WithName("ConfirmEmailChange")
            .WithSummary(
                "Confirm a new email address")
            .WithDescription(
                "Validates the email change token and " +
                "updates the user's email address.")
            .Accepts<ConfirmEmailChangeRequest>(
                "application/json")
            .Produces<ConfirmEmailChangeResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces<Error>(
                StatusCodes.Status404NotFound)
            .Produces<Error>(
                StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        ConfirmEmailChangeRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command =
            new ConfirmEmailChangeCommand(
                request.UserId,
                request.NewEmail,
                request.Token);

        Result<ConfirmEmailChangeResponse> result =
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