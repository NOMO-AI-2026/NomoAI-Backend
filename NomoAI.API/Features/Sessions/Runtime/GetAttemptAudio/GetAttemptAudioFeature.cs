using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;
using System.Security.Claims;

namespace NomoAI.API.Features.Sessions.Runtime.GetAttemptAudio;

public sealed record GetAttemptAudioQuery(int SessionId, int AttemptId, string UserId)
    : IRequest<Result<AttemptAudioResult>>;

public sealed record AttemptAudioResult(byte[] Content, string ContentType);

public sealed class GetAttemptAudioQueryValidator : AbstractValidator<GetAttemptAudioQuery>
{
    public GetAttemptAudioQueryValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.AttemptId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class GetAttemptAudioQueryHandler
    : IRequestHandler<GetAttemptAudioQuery, Result<AttemptAudioResult>>
{
    private readonly AppDbContext _db;

    public GetAttemptAudioQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AttemptAudioResult>> Handle(
        GetAttemptAudioQuery request,
        CancellationToken cancellationToken)
    {
        (ChildOwnershipStatus status, Session? session) = await SessionOwnership.CheckSessionOwnershipAsync(
            _db, request.UserId, request.SessionId, cancellationToken);

        if (status == ChildOwnershipStatus.ChildNotFound || session is null)
        {
            return Result.Failure<AttemptAudioResult>(SessionRuntimeErrors.SessionNotFound);
        }

        if (status == ChildOwnershipStatus.Forbidden)
        {
            return Result.Failure<AttemptAudioResult>(SessionRuntimeErrors.Forbidden);
        }

        var attempt = await _db.SessionAttempts
            .AsNoTracking()
            .Where(a => a.Id == request.AttemptId && a.SessionId == request.SessionId && !a.IsDeleted)
            .Select(a => new
            {
                a.AudioContent,
                a.AudioContentType
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (attempt is null)
        {
            return Result.Failure<AttemptAudioResult>(SessionRuntimeErrors.AttemptNotFound);
        }

        if (attempt.AudioContent is null || attempt.AudioContent.Length == 0)
        {
            return Result.Failure<AttemptAudioResult>(SessionRuntimeErrors.AttemptAudioNotAvailable);
        }

        string contentType = string.IsNullOrWhiteSpace(attempt.AudioContentType)
            ? "application/octet-stream"
            : attempt.AudioContentType;

        return Result.Success(new AttemptAudioResult(attempt.AudioContent, contentType));
    }
}

public sealed class GetAttemptAudioEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sessions/{sessionId:int}/attempts/{attemptId:int}/audio", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor", "Parent"))
            .WithName("GetAttemptAudio")
            .WithSummary("Stream the persisted child attempt audio for doctor/parent review")
            .WithTags("Sessions Runtime")
            .Produces(StatusCodes.Status200OK, contentType: "audio/webm")
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

        Result<AttemptAudioResult> result = await sender.Send(
            new GetAttemptAudioQuery(sessionId, attemptId, userId),
            cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(result.Error, statusCode: result.Error.StatusCode);
        }

        return Results.File(
            result.Value.Content,
            result.Value.ContentType,
            fileDownloadName: null,
            enableRangeProcessing: true);
    }
}
