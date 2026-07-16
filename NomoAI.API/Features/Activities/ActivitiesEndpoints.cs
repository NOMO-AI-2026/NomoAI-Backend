using NomoAI.API.Features.Activities.DeleteActivity;

namespace NomoAI.API.Features.Activities;

public static class ActivitiesEndpoints
{
    public static IEndpointRouteBuilder MapActivitiesEndpoints(
        this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/activities")
            .WithTags("Activities");

        DeleteActivityEndpoint.MapEndpoint(group);

        return app;
    }
}