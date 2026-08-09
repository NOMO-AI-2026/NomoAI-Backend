using Microsoft.EntityFrameworkCore;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Sessions.Runtime;

/// <summary>
/// Owns the CanMakeSession gate between Activity and Session lifecycle.
/// </summary>
internal static class ActivitySessionGate
{
    public static bool IsAvailableForNewSession(Activity activity) =>
        !activity.IsDeleted && activity.CanMakeSession;

    public static void MarkUnavailableAfterCompletedSession(Activity activity)
    {
        activity.CanMakeSession = false;
    }

    public static async Task MarkUnavailableAfterCompletedSessionAsync(
        AppDbContext db,
        int activityId,
        CancellationToken cancellationToken)
    {
        Activity? activity = await db.Activities
            .FirstOrDefaultAsync(
                a => a.Id == activityId && !a.IsDeleted,
                cancellationToken);

        if (activity is null || !activity.CanMakeSession)
        {
            return;
        }

        MarkUnavailableAfterCompletedSession(activity);
    }
}
