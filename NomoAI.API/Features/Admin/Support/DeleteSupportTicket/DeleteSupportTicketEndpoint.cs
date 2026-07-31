using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Admin.Support.DeleteSupportTicket;

public class DeleteSupportTicketEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/admin/support/tickets/{id:int}", HandleAsync)
            .RequireAuthorization(
                policy => policy.RequireRole("Admin"))
            .WithName("DeleteSupportTicket")
            .WithTags("AdminDashboard")
            .WithSummary("Soft-delete a support ticket")
            .WithDescription(
                "Marks a support ticket as deleted. Deleted tickets are excluded from all listings.")
            .Produces(
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
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new DeleteSupportTicketCommand(id);

        Result result =
            await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToProblem();
    }
}
