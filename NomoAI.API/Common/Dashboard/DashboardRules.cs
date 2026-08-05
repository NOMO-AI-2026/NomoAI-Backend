using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Common.Dashboard;

/// <summary>
/// Shared business rules for Doctor/Parent dashboard aggregations (Task 1).
/// Dashboard endpoints (Tasks 2–3) must reuse these definitions.
/// </summary>
public static class DashboardRules
{
    public const int RecentWindowDays = 7;

    /// <summary>
    /// Effective completion timestamp: prefer EndedAt, fallback to StartedAt.
    /// </summary>
    public static DateTime? GetEffectiveCompletedAt(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.EndedAt ?? session.StartedAt;
    }

    public static DateTime GetUtcStartOfToday(DateTime utcNow) =>
        DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);

    public static DateTime GetUtcStartOfTomorrow(DateTime utcNow) =>
        GetUtcStartOfToday(utcNow).AddDays(1);

    public static DateTime GetRecentWindowStartUtc(DateTime utcNow) =>
        utcNow.AddDays(-RecentWindowDays);

    public static bool IsCompletedSession(Session session) =>
        session is { IsDeleted: false, Status: SessionStatus.Completed };

    /// <summary>
    /// Completed session that still needs an explicit doctor review.
    /// </summary>
    public static bool IsAwaitingDoctorReview(Session session) =>
        IsCompletedSession(session) && !session.IsDoctorReviewed;

    public static bool IsCompletedInRange(
        Session session,
        DateTime rangeStartInclusiveUtc,
        DateTime rangeEndExclusiveUtc)
    {
        if (!IsCompletedSession(session))
        {
            return false;
        }

        DateTime? completedAt = GetEffectiveCompletedAt(session);
        if (completedAt is null)
        {
            return false;
        }

        return completedAt.Value >= rangeStartInclusiveUtc &&
               completedAt.Value < rangeEndExclusiveUtc;
    }

    public static bool IsCompletedToday(Session session, DateTime utcNow)
    {
        DateTime start = GetUtcStartOfToday(utcNow);
        DateTime endExclusive = GetUtcStartOfTomorrow(utcNow);
        return IsCompletedInRange(session, start, endExclusive);
    }

    public static bool IsCompletedInLast7Days(Session session, DateTime utcNow)
    {
        if (!IsCompletedSession(session))
        {
            return false;
        }

        DateTime? completedAt = GetEffectiveCompletedAt(session);
        if (completedAt is null)
        {
            return false;
        }

        return completedAt.Value >= GetRecentWindowStartUtc(utcNow) &&
               completedAt.Value <= utcNow;
    }

    /// <summary>
    /// Speech-level change direction using catalog rank (SpeechLevel.Id order for v1).
    /// </summary>
    public static SpeechLevelChangeKind ClassifyLevelChange(
        int previousSpeechLevelId,
        int newSpeechLevelId)
    {
        if (newSpeechLevelId > previousSpeechLevelId)
        {
            return SpeechLevelChangeKind.Progressed;
        }

        if (newSpeechLevelId < previousSpeechLevelId)
        {
            return SpeechLevelChangeKind.Regressed;
        }

        return SpeechLevelChangeKind.Stable;
    }

    /// <summary>
    /// Pending exercise: activity has no completed session yet.
    /// </summary>
    public static bool IsPendingExercise(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (activity.IsDeleted)
        {
            return false;
        }

        if (activity.Sessions is null || activity.Sessions.Count == 0)
        {
            return true;
        }

        return !activity.Sessions.Any(session =>
            !session.IsDeleted &&
            session.Status == SessionStatus.Completed);
    }
}

public enum SpeechLevelChangeKind
{
    Progressed = 0,
    Stable = 1,
    Regressed = 2
}
