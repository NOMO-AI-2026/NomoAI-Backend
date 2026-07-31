using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Admin.Support.GetSupportTicketDetails;

public class GetSupportTicketDetailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/support/tickets/{id:int}", HandleAsync)
            .RequireAuthorization(
                policy => policy.RequireRole("Admin"))
            .WithName("GetSupportTicketDetails")
            .WithTags("AdminDashboard")
            .WithSummary("Get support ticket details")
            .WithDescription(
                "Returns full details of a non-deleted support ticket for admins.")
            .Produces<GetSupportTicketDetailsResponse>(
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
        var query = new GetSupportTicketDetailsQuery(id);

        Result<GetSupportTicketDetailsResponse> result =
            await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
