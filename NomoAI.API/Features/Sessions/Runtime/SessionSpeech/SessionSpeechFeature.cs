using FluentValidation;
using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;
using System.Security.Claims;

namespace NomoAI.API.Features.Sessions.Runtime.SessionSpeech;

public sealed record GetSessionSpeechQuery(int SessionId, string UserId)
    : IRequest<Result<AiSpeechAudioResult>>;

public sealed class GetSessionSpeechQueryValidator : AbstractValidator<GetSessionSpeechQuery>
{
    public GetSessionSpeechQueryValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class GetSessionSpeechQueryHandler
    : IRequestHandler<GetSessionSpeechQuery, Result<AiSpeechAudioResult>>
{
    private readonly AppDbContext _db;
    private readonly IAiCoreClient _aiCoreClient;

    public GetSessionSpeechQueryHandler(AppDbContext db, IAiCoreClient aiCoreClient)
    {
        _db = db;
        _aiCoreClient = aiCoreClient;
    }

    public async Task<Result<AiSpeechAudioResult>> Handle(
        GetSessionSpeechQuery request,
        CancellationToken cancellationToken)
    {
        (ChildOwnershipStatus status, Session? session) = await SessionOwnership.CheckSessionOwnershipAsync(
            _db, request.UserId, request.SessionId, cancellationToken);

        if (status == ChildOwnershipStatus.ChildNotFound || session is null)
        {
            return Result.Failure<AiSpeechAudioResult>(SessionRuntimeErrors.SessionNotFound);
        }

        if (status == ChildOwnershipStatus.Forbidden)
        {
            return Result.Failure<AiSpeechAudioResult>(SessionRuntimeErrors.Forbidden);
        }

        AiSessionPlanV2Response? plan = SessionPlanSnapshot.Deserialize(session.PlanJson);
        AiSessionStepDto? step = plan is null
            ? null
            : SessionPlanSnapshot.GetStep(plan, session.CurrentStepNumber);

        string? spokenText = step?.Avatar?.SpokenText;
        if (string.IsNullOrWhiteSpace(spokenText))
        {
            return Result.Failure<AiSpeechAudioResult>(SessionRuntimeErrors.NoSpokenTextAvailable);
        }

        return await _aiCoreClient.SynthesizeSpeechAsync(spokenText, purpose: "instruction", cancellationToken);
    }
}

public static class SessionSpeechEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapGet("/{sessionId:int}/speech", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor", "Parent"))
            .WithName("GetSessionSpeech")
            .WithSummary("Stream synthesized avatar speech for the current session step")
            .Produces(StatusCodes.Status200OK, contentType: "audio/mpeg")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        int sessionId,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Result<AiSpeechAudioResult> result =
            await sender.Send(new GetSessionSpeechQuery(sessionId, userId), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(result.Error, statusCode: result.Error.StatusCode);
        }

        return Results.Stream(result.Value.Content, result.Value.ContentType);
    }
}
