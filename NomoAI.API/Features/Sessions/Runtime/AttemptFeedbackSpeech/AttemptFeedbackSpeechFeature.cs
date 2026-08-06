using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;
using System.Security.Claims;

namespace NomoAI.API.Features.Sessions.Runtime.AttemptFeedbackSpeech;

public sealed record GetAttemptFeedbackSpeechQuery(int SessionId, int AttemptId, string UserId)
    : IRequest<Result<AiSpeechAudioResult>>;

public sealed class GetAttemptFeedbackSpeechQueryValidator : AbstractValidator<GetAttemptFeedbackSpeechQuery>
{
    public GetAttemptFeedbackSpeechQueryValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.AttemptId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class GetAttemptFeedbackSpeechQueryHandler
    : IRequestHandler<GetAttemptFeedbackSpeechQuery, Result<AiSpeechAudioResult>>
{
    private readonly AppDbContext _db;
    private readonly IAiCoreClient _aiCoreClient;

    public GetAttemptFeedbackSpeechQueryHandler(AppDbContext db, IAiCoreClient aiCoreClient)
    {
        _db = db;
        _aiCoreClient = aiCoreClient;
    }

    public async Task<Result<AiSpeechAudioResult>> Handle(
        GetAttemptFeedbackSpeechQuery request,
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

        SessionAttempts? attempt = await _db.SessionAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == request.AttemptId && a.SessionId == request.SessionId && !a.IsDeleted,
                cancellationToken);

        if (attempt is null)
        {
            return Result.Failure<AiSpeechAudioResult>(SessionRuntimeErrors.AttemptNotFound);
        }

        AttemptEvaluation? evaluation = await _db.AttemptEvaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.AttemptId == attempt.Id && !e.IsDeleted,
                cancellationToken);

        string? spokenText = evaluation?.AvatarSpokenText;
        if (string.IsNullOrWhiteSpace(spokenText))
        {
            return Result.Failure<AiSpeechAudioResult>(SessionRuntimeErrors.FeedbackNotAvailable);
        }

        return await _aiCoreClient.SynthesizeSpeechAsync(spokenText, purpose: "encouragement", cancellationToken);
    }
}

public static class AttemptFeedbackSpeechEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapGet("/{sessionId:int}/attempts/{attemptId:int}/feedback-speech", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor", "Parent"))
            .WithName("GetAttemptFeedbackSpeech")
            .WithSummary("Stream synthesized avatar feedback speech for a completed attempt")
            .Produces(StatusCodes.Status200OK, contentType: "audio/mpeg")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        int sessionId,
        int attemptId,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Result<AiSpeechAudioResult> result = await sender.Send(
            new GetAttemptFeedbackSpeechQuery(sessionId, attemptId, userId), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(result.Error, statusCode: result.Error.StatusCode);
        }

        return Results.Stream(result.Value.Content, result.Value.ContentType);
    }
}
