using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Admin.Support.HandleSupportTicket;

public class HandleSupportTicketEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/admin/support/tickets/{id:int}/action", HandleAsync)
            .RequireAuthorization(
                policy => policy.RequireRole("Admin"))
            .WithName("HandleSupportTicket")
            .WithTags("AdminDashboard")
            .WithSummary("Take action on a support ticket")
            .WithDescription(
                "Updates ticket status and optional admin note. Status cannot be set back to Open.")
            .Accepts<HandleSupportTicketRequest>("application/json")
            .Produces<HandleSupportTicketResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden)
            .Produces<Error>(
                StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        int id,
        HandleSupportTicketRequest request,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? adminUserId = currentUser.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Results.Unauthorized();
        }

        var command = new HandleSupportTicketCommand(
            id,
            request.Status,
            request.AdminNote,
            adminUserId);

        Result<HandleSupportTicketResponse> result =
            await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
