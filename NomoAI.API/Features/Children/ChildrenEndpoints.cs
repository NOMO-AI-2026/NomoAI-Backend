using NomoAI.API.Features.Children.AssignChildToParent;

namespace NomoAI.API.Features.Children;

public static class ChildrenEndpoints
{
    public static IEndpointRouteBuilder MapChildrenEndpoints(
        this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/children")
            .WithTags("Children");

        AssignChildToParentEndpoint.MapEndpoint(group);

        return app;
    }
}