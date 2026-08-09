using Microsoft.EntityFrameworkCore;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary;

/// <summary>
/// Persists analytical session summaries into SessionSummaries for history reopen.
/// </summary>
internal static class SessionSummaryPersister
{
    /// <summary>
    /// After a session becomes Completed, ensure a history row exists.
    /// Safe to call inside the same SaveChanges as session completion.
    /// </summary>
    public static async Task TryPersistForCompletedSessionAsync(
        AppDbContext db,
        Session session,
        CancellationToken cancellationToken)
    {
        if (session.Status != SessionStatus.Completed)
        {
            return;
        }

        IReadOnlyList<SessionSummaryAnalytics.AttemptSignal> signals =
            await SessionSummaryAnalytics.LoadAttemptSignalsAsync(db, session.Id, cancellationToken);

        if (signals.Count == 0)
        {
            return;
        }

        SessionSummary? existing = await db.SessionSummaries
            .FirstOrDefaultAsync(
                s => s.SessionId == session.Id && !s.IsDeleted,
                cancellationToken);

        bool shouldRewrite = existing is null
            || existing.SummaryGenerationMode != "db_analytics"
            || existing.TotalAttempts != signals.Count;

        if (!shouldRewrite)
        {
            return;
        }

        SessionSummaryAnalytics.ComputedAnalytics analytics =
            SessionSummaryAnalytics.Compute(session, signals);

        if (existing is null)
        {
            existing = new SessionSummary
            {
                SessionId = session.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.SessionSummaries.Add(existing);
        }

        SessionSummaryAnalytics.ApplyToEntity(existing, analytics);
    }
}
