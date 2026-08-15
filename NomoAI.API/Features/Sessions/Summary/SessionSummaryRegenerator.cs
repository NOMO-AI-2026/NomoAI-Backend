using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary;

/// <summary>
/// Regenerates a single session's analytics summary without HTTP/auth.
/// Intended for controlled ops/smoke tests (one SessionId at a time).
/// </summary>
internal static class SessionSummaryRegenerator
{
    public sealed record RegenerationResult(
        int SessionId,
        string? PreviousOutcome,
        decimal PreviousAverageScore,
        string NewOutcome,
        decimal NewAverageScore,
        string ShortSummary,
        bool OriginalTargetAchieved);

    public static async Task<RegenerationResult?> RegenerateAsync(
        AppDbContext db,
        int sessionId,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        Session? session = await db.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsDeleted, cancellationToken);

        if (session is null || session.Status != SessionStatus.Completed)
        {
            return null;
        }

        SessionSummary? existing = await db.SessionSummaries
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && !s.IsDeleted, cancellationToken);

        string? previousOutcome = existing?.Outcome;
        decimal previousAverage = existing?.AverageScore ?? 0m;

        IReadOnlyList<SessionSummaryAnalytics.AttemptSignal> signals =
            await SessionSummaryAnalytics.LoadAttemptSignalsAsync(db, sessionId, cancellationToken);

        if (signals.Count == 0)
        {
            return null;
        }

        SessionSummaryAnalytics.ComputedAnalytics analytics =
            SessionSummaryAnalytics.Compute(session, signals, logger);

        if (existing is null)
        {
            existing = new SessionSummary
            {
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow
            };
            db.SessionSummaries.Add(existing);
        }

        SessionSummaryAnalytics.ApplyToEntity(existing, analytics);
        await db.SaveChangesAsync(cancellationToken);

        return new RegenerationResult(
            sessionId,
            previousOutcome,
            previousAverage,
            analytics.Outcome,
            analytics.AverageScore,
            analytics.ShortSummary,
            analytics.OriginalTargetAchieved);
    }
}
