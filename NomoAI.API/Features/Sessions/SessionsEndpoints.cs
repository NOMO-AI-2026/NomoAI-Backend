using NomoAI.API.Features.Sessions.Ai.CreateSessionSummary;
using NomoAI.API.Features.Sessions.Ai.EvaluateAttempt;
using NomoAI.API.Features.Sessions.Ai.PlanSession;

namespace NomoAI.API.Features.Sessions;

public static class SessionsEndpoints
{
    public static IEndpointRouteBuilder MapSessionsEndpoints(
        this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/sessions/ai")
            .WithTags("Sessions AI");

        PlanSessionEndpoint.MapEndpoint(group);
        EvaluateAttemptEndpoint.MapEndpoint(group);
        CreateSessionSummaryEndpoint.MapEndpoint(group);

        return app;
    }
}
