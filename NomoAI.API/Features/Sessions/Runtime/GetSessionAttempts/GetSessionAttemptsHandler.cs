using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Sessions.Summary;
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

        bool isDoctor = await _db.Doctor
            .AsNoTracking()
            .AnyAsync(d => d.UserId == request.UserId && !d.IsDeleted, cancellationToken);

        if (!isDoctor)
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
                    .Select(e => new
                    {
                        e.AccuracyScore,
                        e.FluencyScore,
                        e.PronunciationScore,
                        e.CompletenessScore,
                        e.IsSuccessful,
                        e.SpeechOutcome,
                        e.AdaptiveAction,
                        e.Matched,
                        e.Feedback,
                        e.AvatarSpokenText,
                        e.AvatarEmotion,
                        e.NormalizedTranscript,
                        e.EvaluationJson
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
                Evaluation = a.Evaluation is null
                    ? null
                    : MapEvaluation(a.Evaluation.AccuracyScore,
                        a.Evaluation.FluencyScore,
                        a.Evaluation.PronunciationScore,
                        a.Evaluation.CompletenessScore,
                        a.Evaluation.IsSuccessful,
                        a.Evaluation.SpeechOutcome,
                        a.Evaluation.AdaptiveAction,
                        a.Evaluation.Matched,
                        a.Evaluation.Feedback,
                        a.Evaluation.AvatarSpokenText,
                        a.Evaluation.AvatarEmotion,
                        a.Evaluation.NormalizedTranscript,
                        a.Evaluation.EvaluationJson),
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

    private static SessionAttemptEvaluationResponse MapEvaluation(
        decimal accuracy,
        decimal? fluency,
        decimal? pronunciation,
        decimal completeness,
        bool isSuccessful,
        string? speechOutcome,
        string? adaptiveAction,
        bool? matched,
        string? feedback,
        string? avatarSpokenText,
        string? avatarEmotion,
        string? normalizedTranscript,
        string? evaluationJson)
    {
        double? aiOverall = null;
        if (!string.IsNullOrWhiteSpace(evaluationJson))
        {
            try
            {
                var snapshot = System.Text.Json.JsonSerializer.Deserialize<Common.Ai.Contracts.AiEvaluateAttemptV2Response>(
                    evaluationJson,
                    Infrastructure.Ai.AiCoreJsonSerializerOptions.Instance);
                aiOverall = snapshot?.Scores?.Overall ?? snapshot?.SpeechAnalysis?.Scores?.OverallScore;
            }
            catch (System.Text.Json.JsonException)
            {
                // fall through to component average
            }
        }

        decimal? overall = EvidenceAwareScoreMath.AverageAvailable(
            accuracy,
            completeness,
            fluency,
            pronunciation);

        if (aiOverall is double o)
        {
            overall = Convert.ToDecimal(EvidenceAwareScoreMath.NormalizeScore(o) ?? o);
        }

        return new SessionAttemptEvaluationResponse
        {
            AccuracyScore = accuracy,
            FluencyScore = fluency,
            PronunciationScore = pronunciation,
            CompletenessScore = completeness,
            OverallScore = overall ?? 0m,
            IsSuccessful = isSuccessful,
            SpeechOutcome = speechOutcome,
            AdaptiveAction = adaptiveAction,
            Matched = matched,
            Feedback = feedback,
            AvatarSpokenText = avatarSpokenText,
            AvatarEmotion = avatarEmotion,
            NormalizedTranscript = normalizedTranscript
        };
    }
}
