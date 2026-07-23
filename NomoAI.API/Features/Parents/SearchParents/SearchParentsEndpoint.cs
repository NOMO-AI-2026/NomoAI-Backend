using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Parents.SearchParents;

public static class SearchParentsEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapGet("/search", HandleAsync)
            //.RequireAuthorization(policy =>
            //    policy.RequireRole("Admin"))
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
        var query = new SearchParentsQuery(
            request.SearchTerm,
            request.PageNumber ?? 1,
            request.PageSize ?? 10);

        Result<SearchParentsResponse> result =
            await sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(
                result.Error,
                statusCode: result.Error.StatusCode);
        }

        return Results.Ok(result.Value);
    }
}