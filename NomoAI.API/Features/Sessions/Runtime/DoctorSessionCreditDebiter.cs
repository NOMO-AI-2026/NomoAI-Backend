using Microsoft.EntityFrameworkCore;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Sessions.Runtime;

/// <summary>
/// Deducts purchased therapy minutes from the doctor's wallet when a child session completes.
/// Primary billable time = actual duration (StartedAt/CreatedAt → EndedAt).
/// Capped by EstimatedDurationMinutes so a forgotten open session cannot drain the wallet.
/// </summary>
internal static class DoctorSessionCreditDebiter
{
    /// <summary>
    /// Idempotent: one SessionUsage transaction per session.
    /// Does not fail session completion if the wallet is missing or already empty.
    /// </summary>
    public static async Task TryDebitForCompletedSessionAsync(
        AppDbContext db,
        Session session,
        CancellationToken cancellationToken)
    {
        if (session.Status != SessionStatus.Completed)
        {
            return;
        }

        bool alreadyDebited = await db.DoctorTransactions
            .AnyAsync(
                t => t.SessionId == session.Id
                    && t.Type == TransactionType.SessionUsage
                    && !t.IsDeleted,
                cancellationToken);

        if (alreadyDebited)
        {
            return;
        }

        var billing = await db.Sessions
            .AsNoTracking()
            .Where(s => s.Id == session.Id && !s.IsDeleted)
            .Select(s => new
            {
                DoctorId = s.Child.DoctorId,
                EstimatedMinutes = s.Activity.EstimatedDurationMinutes
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (billing is null || billing.DoctorId <= 0)
        {
            return;
        }

        // Prefer StartedAt; fall back to CreatedAt if start was never stamped.
        // EndedAt may not be flushed to DB yet — use the tracked entity values.
        DateTime? startedAt = session.StartedAt ?? session.CreatedAt;
        DateTime? endedAt = session.EndedAt ?? DateTime.UtcNow;

        int actualMinutes = CalculateBillableMinutes(startedAt, endedAt);
        int requestedMinutes = ResolveBillableMinutes(actualMinutes, billing.EstimatedMinutes);
        if (requestedMinutes <= 0)
        {
            return;
        }

        DoctorCreditWallet? wallet = await db.DoctorCreditWallets
            .FirstOrDefaultAsync(
                w => w.DoctorId == billing.DoctorId && !w.IsDeleted,
                cancellationToken);

        if (wallet is null)
        {
            return;
        }

        (int newBalance, int debitedMinutes) = ApplyDebit(wallet.AvailableMinutes, requestedMinutes);

        wallet.AvailableMinutes = newBalance;
        wallet.UpdatedAtUtc = DateTime.UtcNow;

        db.DoctorTransactions.Add(new DoctorTransaction
        {
            DoctorId = billing.DoctorId,
            Type = TransactionType.SessionUsage,
            Minutes = debitedMinutes,
            BalanceAfter = newBalance,
            SessionId = session.Id,
            PlanPurchaseId = null
        });
    }

    /// <summary>
    /// Actual elapsed session time in whole minutes (rounded up).
    /// Short sessions bill only what was used (minimum 1 minute when duration &gt; 0).
    /// </summary>
    internal static int CalculateBillableMinutes(DateTime? startedAt, DateTime? endedAt)
    {
        if (startedAt is null || endedAt is null)
        {
            return 0;
        }

        TimeSpan duration = endedAt.Value - startedAt.Value;
        if (duration <= TimeSpan.Zero)
        {
            return 0;
        }

        int minutes = (int)Math.Ceiling(duration.TotalMinutes);
        return Math.Max(1, minutes);
    }

    /// <summary>
    /// Bill actual minutes, but never more than the activity estimate when an estimate exists.
    /// Keeps short sessions cheap and protects against abandoned long-running sessions.
    /// </summary>
    internal static int ResolveBillableMinutes(int actualMinutes, int estimatedDurationMinutes)
    {
        int actual = Math.Max(0, actualMinutes);
        int estimated = Math.Max(0, estimatedDurationMinutes);

        if (actual <= 0)
        {
            return 0;
        }

        if (estimated <= 0)
        {
            return actual;
        }

        return Math.Min(actual, estimated);
    }

    /// <summary>
    /// Floors the wallet at zero. Returns the new balance and how many minutes were actually removed.
    /// </summary>
    internal static (int NewBalance, int DebitedMinutes) ApplyDebit(int availableMinutes, int requestedMinutes)
    {
        int available = Math.Max(0, availableMinutes);
        int requested = Math.Max(0, requestedMinutes);
        int debited = Math.Min(available, requested);
        return (available - debited, debited);
    }
}
