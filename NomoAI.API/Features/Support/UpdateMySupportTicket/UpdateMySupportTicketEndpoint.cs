using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Support.UpdateMySupportTicket;

public class UpdateMySupportTicketEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/support/tickets/{id:int}", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor", "Parent"))
            .WithName("UpdateMySupportTicket")
            .WithTags("Support")
            .WithSummary("Update my support ticket")
            .WithDescription(
                "Allows the ticket owner to update subject and message " +
                "only while the ticket is Open and has not been handled by an admin.")
            .Accepts<UpdateMySupportTicketRequest>("application/json")
            .Produces<UpdateMySupportTicketResponse>(
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
        UpdateMySupportTicketRequest request,
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

        var command = new UpdateMySupportTicketCommand(
            id,
            userId,
            request.Subject,
            request.Message);

        Result<UpdateMySupportTicketResponse> result =
            await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
