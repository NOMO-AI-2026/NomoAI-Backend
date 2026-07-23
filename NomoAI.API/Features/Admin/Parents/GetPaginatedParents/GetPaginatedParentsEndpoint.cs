using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Admin.Parents.GetPaginatedParents;

public static class GetPaginatedParentsEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapGet("/parents", HandleAsync)
            .RequireAuthorization(
                policy => policy.RequireRole("Admin"))
            .WithName("GetPaginatedParents")
            .WithTags("AdminDashboard")
            .WithSummary(
                "Get paginated parent list")
            .WithDescription(
                "Returns a paginated list of active parents for the admin dashboard.")
            .Produces<GetPaginatedParentsResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] GetPaginatedParentsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetPaginatedParentsQuery(
            request.PageNumber ?? 1,
            request.PageSize ?? 10);

        Result<GetPaginatedParentsResponse> result =
            await sender.Send(
                query,
                cancellationToken);

        if (result.IsFailure)
        {
            return
                result.ToProblem();
                
        }

        return Results.Ok(result);
    }
}