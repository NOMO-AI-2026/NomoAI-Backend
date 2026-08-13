using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Sessions.Runtime;

/// <summary>
/// Therapy session lifecycle: InProgress from the moment a session starts
/// until the plan actually finishes, then Completed. Scheduled is only the
/// unused default (enum 0) for rows that never started.
/// </summary>
internal static class SessionLifecycle
{
    public static void MarkStarted(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Status is SessionStatus.Completed or SessionStatus.Cancelled or SessionStatus.Missed)
        {
            return;
        }

        session.Status = SessionStatus.InProgress;
        session.StartedAt ??= DateTime.UtcNow;
        session.EndedAt = null;
    }

    public static void MarkCompleted(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Status == SessionStatus.Completed)
        {
            session.EndedAt ??= DateTime.UtcNow;
            return;
        }

        session.Status = SessionStatus.Completed;
        session.EndedAt = DateTime.UtcNow;
    }

    public static bool IsRunnable(Session session) =>
        ResolveEffectiveStatus(session) == SessionStatus.InProgress;

    public static SessionStatus ResolveEffectiveStatus(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return ResolveEffectiveStatus(
            session.Status,
            session.StartedAt,
            session.EndedAt,
            session.PlanJson);
    }

    public static SessionStatus ResolveEffectiveStatus(
        SessionStatus status,
        DateTime? startedAt,
        DateTime? endedAt,
        string? planJson)
    {
        if (status is SessionStatus.Completed or SessionStatus.Cancelled or SessionStatus.Missed)
        {
            return status;
        }

        if (endedAt is not null)
        {
            return SessionStatus.Completed;
        }

        if (status == SessionStatus.InProgress)
        {
            return SessionStatus.InProgress;
        }

        // Default enum 0 (Scheduled) but the child already started this session.
        if (startedAt is not null || !string.IsNullOrWhiteSpace(planJson))
        {
            return SessionStatus.InProgress;
        }

        return SessionStatus.Scheduled;
    }

    /// <summary>
    /// Aligns persisted Status with the real lifecycle. Completes the session
    /// when the current plan step no longer exists (plan finished).
    /// </summary>
    public static bool SynchronizePersistedStatus(Session session, AiSessionPlanV2Response? plan)
    {
        ArgumentNullException.ThrowIfNull(session);

        SessionStatus effective = ResolveEffectiveStatus(session);
        bool changed = false;

        if (session.Status != effective)
        {
            if (effective == SessionStatus.InProgress)
            {
                MarkStarted(session);
            }
            else if (effective == SessionStatus.Completed)
            {
                MarkCompleted(session);
            }
            else
            {
                session.Status = effective;
            }

            changed = true;
        }

        if (session.Status == SessionStatus.InProgress &&
            plan is not null &&
            SessionPlanSnapshot.GetStep(plan, session.CurrentStepNumber) is null)
        {
            MarkCompleted(session);
            changed = true;
        }

        return changed;
    }
}
