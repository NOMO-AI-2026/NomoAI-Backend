using NomoAI.API.Features.Parents.SearchParents;

namespace NomoAI.API.Features.Parents;

public static class ParentsEndpoints
{
    public static IEndpointRouteBuilder MapParentsEndpoints(
        this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/parents")
            .WithTags("Parents");

        SearchParentsEndpoint.MapEndpoint(group);

        return app;
    }
}