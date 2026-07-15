using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ResendEmailConfirmation;

public static class ResendEmailConfirmationEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/resend-email-confirmation", HandleAsync)
            .AllowAnonymous()
            .WithName("ResendEmailConfirmation")
            .WithSummary("Resend email confirmation")
            .WithDescription(
                "Sends a new email confirmation link if the email belongs to an unconfirmed account.")
            .Produces<ResendEmailConfirmationResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(ResendEmailConfirmationCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        return Results.Ok(new ResendEmailConfirmationResponse("If an account with this email exists and is unconfirmed, a confirmation email has been sent."));
    }
}

public sealed record ResendEmailConfirmationResponse(string Message);