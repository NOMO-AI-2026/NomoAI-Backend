using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Sessions.Summary;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Sessions.Runtime;

/// <summary>
/// Completes a therapy session and runs the one-time follow-up work
/// (activity gate, credit debit, summary). Safe to call more than once.
/// </summary>
internal static class SessionCompletion
{
    public static async Task FinalizeAsync(
        AppDbContext db,
        Session session,
        CancellationToken cancellationToken)
    {
        SessionLifecycle.MarkCompleted(session);

        await ActivitySessionGate.MarkUnavailableAfterCompletedSessionAsync(
            db,
            session.ActivityId,
            cancellationToken);
        await DoctorSessionCreditDebiter.TryDebitForCompletedSessionAsync(
            db,
            session,
            cancellationToken);
        await SessionSummaryPersister.TryPersistForCompletedSessionAsync(
            db,
            session,
            cancellationToken);
    }
}
