using NomoAI.API.Features.Admin.Parents.GetPaginatedParents;

namespace NomoAI.API.Features.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(
        this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/admin")
            .WithTags("Admin");

        GetPaginatedParentsEndpoint.MapEndpoint(group);

        return app;
    }
}