using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Infrastructure.Ai;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary;

internal static class SessionSummaryRequestFactory
{
    public static async Task<(AiSessionSummaryRequest? Request, string? ErrorCode)> BuildAsync(
        AppDbContext db,
        Session session,
        CancellationToken cancellationToken)
    {
        List<(SessionAttempts Attempt, AttemptEvaluation Evaluation)> rows = await (
                from a in db.SessionAttempts.AsNoTracking()
                join e in db.AttemptEvaluations.AsNoTracking() on a.Id equals e.AttemptId
                where a.SessionId == session.Id && !a.IsDeleted && !e.IsDeleted
                orderby a.AttemptNumber, a.Id
                select new ValueTuple<SessionAttempts, AttemptEvaluation>(a, e))
            .ToListAsync(cancellationToken);

        // 1 DB attempt → 1 summary attempt (dedupe join accidents).
        rows = rows
            .GroupBy(r => r.Attempt.Id)
            .Select(g => g.First())
            .OrderBy(r => r.Attempt.AttemptNumber)
            .ThenBy(r => r.Attempt.Id)
            .ToList();

        if (rows.Count == 0)
        {
            return (null, "no_attempts");
        }

        var attempts = new List<AiSummaryAttemptInput>(rows.Count);
        foreach ((SessionAttempts attempt, AttemptEvaluation evaluation) in rows)
        {
            AiEvaluateAttemptV2Response? snapshot = DeserializeEvaluation(evaluation.EvaluationJson);
            bool snapshotHadScores = snapshot?.Scores is not null || snapshot?.SpeechAnalysis?.Scores is not null;

            double? accuracy = snapshot?.Scores?.Accuracy
                ?? snapshot?.SpeechAnalysis?.Scores?.AccuracyScore
                ?? (double)evaluation.AccuracyScore;

            double? completeness = snapshot?.Scores?.Completeness
                ?? snapshot?.SpeechAnalysis?.Scores?.CompletenessScore
                ?? (double)evaluation.CompletenessScore;

            double? fluency = snapshot?.Scores?.Fluency
                ?? snapshot?.SpeechAnalysis?.Scores?.FluencyScore;
            if (fluency is null && !snapshotHadScores && evaluation.FluencyScore is decimal dbFluency and not 0)
            {
                fluency = (double)dbFluency;
            }

            double? pronunciation = snapshot?.Scores?.Pronunciation
                ?? snapshot?.SpeechAnalysis?.Scores?.PronunciationProxyScore;
            if (pronunciation is null && !snapshotHadScores && evaluation.PronunciationScore is decimal dbPron and not 0)
            {
                pronunciation = (double)dbPron;
            }

            double? overall = EvidenceAwareScoreMath.ResolveOverall(
                snapshot?.Scores?.Overall ?? snapshot?.SpeechAnalysis?.Scores?.OverallScore,
                accuracy,
                completeness,
                fluency,
                pronunciation);

            string speechOutcome = evaluation.SpeechOutcome
                ?? snapshot?.SpeechOutcome
                ?? "scored";

            if (speechOutcome.Trim().ToLowerInvariant() is "no_speech" or "no_speech_detected" or "empty_transcription")
            {
                overall = null;
            }

            attempts.Add(new AiSummaryAttemptInput
            {
                AttemptNumber = attempt.AttemptNumber,
                OverallScore = overall,
                AccuracyScore = accuracy,
                CompletenessScore = completeness,
                FluencyScore = fluency,
                PronunciationProxyScore = pronunciation,
                SpeechOutcome = speechOutcome,
                AdaptiveAction = evaluation.AdaptiveAction
                    ?? snapshot?.AdaptiveDecision?.Action
                    ?? "retry_same",
                ReasonCodes = snapshot?.AdaptiveDecision?.ReasonCodes?.ToArray()
                    ?? Array.Empty<string>(),
                KnowledgeSourceIds = ParseGuidList(evaluation.KnowledgeSourceIdsJson)
            });
        }

        IReadOnlyList<Guid>? planSources = ParseGuidList(session.KnowledgeSourceIdsJson);

        var request = new AiSessionSummaryRequest
        {
            SessionId = session.Id.ToString(),
            ActivityId = session.ActivityId.ToString(),
            ActivityType = string.IsNullOrWhiteSpace(session.ActivityType) ? "word" : session.ActivityType,
            TargetValue = string.IsNullOrWhiteSpace(session.Prompt) ? session.SessionTitle : session.Prompt!,
            SpeechLevel = string.IsNullOrWhiteSpace(session.SpeechLevel) ? "unknown" : session.SpeechLevel!,
            Age = session.ChildAge is int age and > 0 ? Math.Clamp(age, 1, 18) : 6,
            Language = string.IsNullOrWhiteSpace(session.Language) ? "ar" : session.Language,
            Attempts = attempts,
            ChildId = session.ChildId.ToString(),
            StartedAt = session.StartedAt is DateTime started
                ? new DateTimeOffset(DateTime.SpecifyKind(started, DateTimeKind.Utc))
                : null,
            CompletedAt = session.EndedAt is DateTime ended
                ? new DateTimeOffset(DateTime.SpecifyKind(ended, DateTimeKind.Utc))
                : DateTimeOffset.UtcNow,
            MaximumAttempts = null,
            SessionPlanSourceIds = planSources,
            AdditionalContext = session.RequiresDoctorReview
                ? "Session flagged for doctor review during adaptive therapy."
                : null
        };

        return (request, null);
    }

    private static AiEvaluateAttemptV2Response? DeserializeEvaluation(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiEvaluateAttemptV2Response>(
                json,
                AiCoreJsonSerializerOptions.Instance);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<Guid>? ParseGuidList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            List<Guid>? list = JsonSerializer.Deserialize<List<Guid>>(json, AiCoreJsonSerializerOptions.Instance);
            return list is { Count: > 0 } ? list : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
