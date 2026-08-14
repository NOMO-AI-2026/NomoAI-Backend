using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Infrastructure.Ai;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary;

/// <summary>
/// Dev/diagnostic end-to-end trace for one session summary pipeline.
/// Does not log transcripts, audio, or child PII by default.
/// </summary>
internal static class SessionSummaryPipelineDiagnostics
{
    public sealed record AttemptTraceRow(
        int SessionId,
        int AttemptId,
        int AttemptNumber,
        string? OriginalTarget,
        string? PracticeTarget,
        bool? TargetMatched,
        double? AccuracyScore,
        double? CompletenessScore,
        double? FluencyScore,
        double? PronunciationScore,
        double? OverallScore,
        string? AdaptiveAction,
        int HintsUsed,
        int SimplificationLevel,
        string? SessionPhase,
        bool AudioExists,
        bool HasTranscript);

    public sealed record StageTrace(
        IReadOnlyList<AttemptTraceRow> DbAttempts,
        int DistinctAttemptCount,
        AiSessionSummaryRequest? SummaryRequest,
        SessionSummaryAnalytics.ComputedAnalytics? Analytics,
        SessionSummary? PersistedSummary);

    public static async Task<StageTrace> TraceAsync(
        AppDbContext db,
        int sessionId,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        Session? session = await db.Sessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsDeleted, cancellationToken);

        var rows = await (
                from a in db.SessionAttempts.AsNoTracking()
                join e in db.AttemptEvaluations.AsNoTracking() on a.Id equals e.AttemptId
                where a.SessionId == sessionId && !a.IsDeleted && !e.IsDeleted
                orderby a.AttemptNumber
                select new { a, e })
            .ToListAsync(cancellationToken);

        rows = rows.GroupBy(r => r.a.Id).Select(g => g.First()).OrderBy(r => r.a.AttemptNumber).ToList();

        var attemptRows = new List<AttemptTraceRow>(rows.Count);
        foreach (var row in rows)
        {
            AiEvaluateAttemptV2Response? snap = null;
            if (!string.IsNullOrWhiteSpace(row.e.EvaluationJson))
            {
                try
                {
                    snap = JsonSerializer.Deserialize<AiEvaluateAttemptV2Response>(
                        row.e.EvaluationJson,
                        AiCoreJsonSerializerOptions.Instance);
                }
                catch (JsonException)
                {
                    // ignore
                }
            }

            AiSessionExecutionDto? exec = snap?.SessionExecution;
            attemptRows.Add(new AttemptTraceRow(
                sessionId,
                row.a.Id,
                row.a.AttemptNumber,
                exec?.OriginalTarget ?? snap?.AdaptiveDecision?.OriginalTarget,
                exec?.PracticeTarget ?? snap?.AdaptiveDecision?.PracticeTarget,
                row.e.Matched ?? snap?.Scores?.Matched,
                snap?.Scores?.Accuracy ?? (double)row.e.AccuracyScore,
                snap?.Scores?.Completeness ?? (double)row.e.CompletenessScore,
                snap?.Scores?.Fluency ?? (double?)row.e.FluencyScore,
                snap?.Scores?.Pronunciation ?? (double?)row.e.PronunciationScore,
                snap?.Scores?.Overall
                    ?? EvidenceAwareScoreMath.AverageAvailable(
                        (double)row.e.AccuracyScore,
                        (double)row.e.CompletenessScore,
                        (double?)row.e.FluencyScore,
                        (double?)row.e.PronunciationScore),
                row.e.AdaptiveAction,
                exec?.HintsUsed ?? snap?.AdaptiveDecision?.HintsUsed ?? 0,
                exec?.SimplificationLevel ?? snap?.AdaptiveDecision?.SimplificationLevel ?? 0,
                exec?.Phase ?? snap?.AdaptiveDecision?.SessionPhase,
                AudioExists: row.a.AudioContent != null || !string.IsNullOrWhiteSpace(row.a.AudioUrl),
                HasTranscript: !string.IsNullOrWhiteSpace(row.e.NormalizedTranscript)));
        }

        AiSessionSummaryRequest? request = null;
        SessionSummaryAnalytics.ComputedAnalytics? analytics = null;
        if (session is not null)
        {
            (request, _) = await SessionSummaryRequestFactory.BuildAsync(db, session, cancellationToken);
            IReadOnlyList<SessionSummaryAnalytics.AttemptSignal> signals =
                await SessionSummaryAnalytics.LoadAttemptSignalsAsync(db, sessionId, cancellationToken);
            analytics = SessionSummaryAnalytics.Compute(session, signals, logger);
        }

        SessionSummary? summary = await db.SessionSummaries.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && !s.IsDeleted, cancellationToken);

        logger?.LogInformation(
            "SessionSummaryPipelineTrace SessionId={SessionId} DbAttemptCount={DbAttemptCount} DistinctAttemptCount={Distinct} SummaryRequestAttempts={RequestAttempts} AnalyticsOutcome={Outcome} PersistedAverage={Average}",
            sessionId,
            attemptRows.Count,
            attemptRows.Select(a => a.AttemptId).Distinct().Count(),
            request?.Attempts.Count ?? 0,
            analytics?.Outcome,
            summary?.AverageScore);

        return new StageTrace(
            attemptRows,
            attemptRows.Select(a => a.AttemptId).Distinct().Count(),
            request,
            analytics,
            summary);
    }
}
