using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Support.DeleteMySupportTicket;

public class DeleteMySupportTicketEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/support/tickets/{id:int}", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor", "Parent"))
            .WithName("DeleteMySupportTicket")
            .WithTags("Support")
            .WithSummary("Soft-delete my support ticket")
            .WithDescription(
                "Allows the ticket owner to soft-delete a ticket " +
                "only while it is Open and has not been handled by an admin.")
            .Produces(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces<Error>(
                StatusCodes.Status403Forbidden)
            .Produces<Error>(
                StatusCodes.Status404NotFound)
            .Produces<Error>(
                StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        int id,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var command = new DeleteMySupportTicketCommand(id, userId);

        Result result =
            await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToProblem();
    }
}
