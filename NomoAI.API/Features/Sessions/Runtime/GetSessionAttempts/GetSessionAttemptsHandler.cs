using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Sessions.Runtime.GetSessionAttempts;

internal sealed class GetSessionAttemptsHandler
    : IRequestHandler<GetSessionAttemptsQuery, Result<GetSessionAttemptsResponse>>
{
    private readonly AppDbContext _db;

    public GetSessionAttemptsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<GetSessionAttemptsResponse>> Handle(
        GetSessionAttemptsQuery request,
        CancellationToken cancellationToken)
    {
        (ChildOwnershipStatus status, Session? session) =
            await SessionOwnership.CheckSessionOwnershipAsync(
                _db, request.UserId, request.SessionId, cancellationToken);

        if (status == ChildOwnershipStatus.ChildNotFound || session is null)
        {
            return Result.Failure<GetSessionAttemptsResponse>(SessionRuntimeErrors.SessionNotFound);
        }

        if (status == ChildOwnershipStatus.Forbidden)
        {
            return Result.Failure<GetSessionAttemptsResponse>(SessionRuntimeErrors.Forbidden);
        }

        var rows = await _db.SessionAttempts
            .AsNoTracking()
            .Where(a => a.SessionId == session.Id && !a.IsDeleted)
            .OrderBy(a => a.AttemptNumber)
            .ThenBy(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.AttemptNumber,
                a.AudioUrl,
                HasAudio = a.AudioContent != null,
                a.CreatedAt,
                Evaluation = _db.AttemptEvaluations
                    .Where(e => e.AttemptId == a.Id && !e.IsDeleted)
                    .Select(e => new SessionAttemptEvaluationResponse
                    {
                        AccuracyScore = e.AccuracyScore,
                        FluencyScore = e.FluencyScore,
                        PronunciationScore = e.PronunciationScore,
                        CompletenessScore = e.CompletenessScore,
                        OverallScore = e.AccuracyScore + e.FluencyScore
                            + e.PronunciationScore + e.CompletenessScore,
                        IsSuccessful = e.IsSuccessful,
                        SpeechOutcome = e.SpeechOutcome,
                        AdaptiveAction = e.AdaptiveAction,
                        Matched = e.Matched,
                        Feedback = e.Feedback,
                        AvatarSpokenText = e.AvatarSpokenText,
                        AvatarEmotion = e.AvatarEmotion,
                        NormalizedTranscript = e.NormalizedTranscript
                    })
                    .FirstOrDefault(),
                Transcription = _db.AttemptTranscribtions
                    .Where(t => t.AttemptId == a.Id && !t.IsDeleted)
                    .Select(t => new SessionAttemptTranscriptionResponse
                    {
                        TranscribedText = t.TranscribedText,
                        NormalizedText = t.NormalizedText,
                        DetectedLanguage = t.DetectedLanguage
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        List<SessionAttemptItemResponse> attempts = rows
            .Select(a => new SessionAttemptItemResponse
            {
                AttemptId = a.Id,
                AttemptNumber = a.AttemptNumber,
                AudioUrl = !string.IsNullOrWhiteSpace(a.AudioUrl)
                    ? a.AudioUrl
                    : a.HasAudio
                        ? AttemptAudioUrl.BuildRelativePath(session.Id, a.Id)
                        : null,
                CreatedAt = a.CreatedAt,
                Evaluation = a.Evaluation,
                Transcription = a.Transcription
            })
            .ToList();

        return Result.Success(new GetSessionAttemptsResponse
        {
            SessionId = session.Id,
            ChildId = session.ChildId,
            TotalAttempts = attempts.Count,
            Attempts = attempts
        });
    }
}
