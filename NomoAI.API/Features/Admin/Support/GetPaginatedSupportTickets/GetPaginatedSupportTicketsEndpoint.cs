using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Admin.Support.GetPaginatedSupportTickets;

public class GetPaginatedSupportTicketsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/support/tickets", HandleAsync)
            .RequireAuthorization(
                policy => policy.RequireRole("Admin"))
            .WithName("GetPaginatedSupportTickets")
            .WithTags("AdminDashboard")
            .WithSummary("Get paginated support tickets")
            .WithDescription(
                "Returns a paginated list of non-deleted support tickets for admins. Optional status filter.")
            .Produces<GetPaginatedSupportTicketsResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] GetPaginatedSupportTicketsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetPaginatedSupportTicketsQuery(
            request.PageNumber ?? 1,
            request.PageSize ?? 10,
            request.Status);

        Result<GetPaginatedSupportTicketsResponse> result =
            await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
