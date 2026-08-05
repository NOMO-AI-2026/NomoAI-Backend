using NomoAI.API.Features.Sessions.Ai.CreateSessionSummary;
using NomoAI.API.Features.Sessions.Ai.EvaluateAttempt;
using NomoAI.API.Features.Sessions.Ai.PlanSession;
using NomoAI.API.Features.Sessions.Runtime.AttemptFeedbackSpeech;
using NomoAI.API.Features.Sessions.Runtime.ContinueStep;
using NomoAI.API.Features.Sessions.Runtime.GetSessionRuntime;
using NomoAI.API.Features.Sessions.Runtime.SessionSpeech;
using NomoAI.API.Features.Sessions.Runtime.StartSession;
using NomoAI.API.Features.Sessions.Runtime.SubmitAttempt;

namespace NomoAI.API.Features.Sessions;

public static class SessionsEndpoints
{
    public static IEndpointRouteBuilder MapSessionsEndpoints(
        this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder aiGroup = app
            .MapGroup("/api/sessions/ai")
            .WithTags("Sessions AI");

        PlanSessionEndpoint.MapEndpoint(aiGroup);
        EvaluateAttemptEndpoint.MapEndpoint(aiGroup);
        CreateSessionSummaryEndpoint.MapEndpoint(aiGroup);

        // Session-runtime vertical slice: persisted sessions driving the React avatar UI.
        RouteGroupBuilder runtimeGroup = app
            .MapGroup("/api/sessions")
            .WithTags("Sessions Runtime");

        StartSessionEndpoint.MapEndpoint(runtimeGroup);
        GetSessionRuntimeEndpoint.MapEndpoint(runtimeGroup);
        ContinueSessionStepEndpoint.MapEndpoint(runtimeGroup);
        SessionSpeechEndpoint.MapEndpoint(runtimeGroup);
        AttemptFeedbackSpeechEndpoint.MapEndpoint(runtimeGroup);
        SubmitAttemptEndpoint.MapEndpoint(runtimeGroup);

        return app;
    }
}
