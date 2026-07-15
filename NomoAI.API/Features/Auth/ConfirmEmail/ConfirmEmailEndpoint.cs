using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ConfirmEmail;

public static class ConfirmEmailEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/confirm-email", HandleAsync)
            .AllowAnonymous()
            .WithName("ConfirmEmail")
            .WithSummary("Confirm user email")
            .WithDescription(
                "Validates the email confirmation token and confirms the user's email address.")
            .Produces<ConfirmEmailResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(ConfirmEmailCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }
        
        return Results.Ok(result);
    }
}

public sealed record ConfirmEmailResponse(string Message);