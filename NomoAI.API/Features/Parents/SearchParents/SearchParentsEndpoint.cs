using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Parents.SearchParents;

public static class SearchParentsEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapGet("/search", HandleAsync)
            .AllowAnonymous()
            .WithName("SearchParents")
            .WithSummary("Search for parents")
            .WithDescription(
                "Searches for active parent accounts by full name, email, or phone number.")
            .Produces<SearchParentsResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] SearchParentsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        int pageNumber = request.PageNumber ?? 1;
        int pageSize = request.PageSize ?? 10;

        var query = new SearchParentsQuery(
            request.SearchTerm,
            pageNumber,
            pageSize);

        Result<SearchParentsResponse> result =
            await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(result.Error);
        }

        return Results.Ok(result.Value);
    }
}