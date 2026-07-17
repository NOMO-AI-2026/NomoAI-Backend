using NomoAI.API.Features.Activities.CreateActivity;
using NomoAI.API.Features.Activities.DeleteActivity;
using NomoAI.API.Features.Activities.UpdateActivity;

namespace NomoAI.API.Features.Activities;

public static class ActivitiesEndpoints
{
    public static IEndpointRouteBuilder MapActivitiesEndpoints(
        this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/activities")
            .WithTags("Activities");

        CreateActivityEndpoint.MapEndpoint(group);
        UpdateActivityEndpoint.MapEndpoint(group);
        DeleteActivityEndpoint.MapEndpoint(group);

        return app;
    }
}