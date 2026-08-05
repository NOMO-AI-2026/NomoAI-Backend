using Microsoft.EntityFrameworkCore;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Sessions.Runtime;

internal enum ChildOwnershipStatus
{
    ChildNotFound,
    Forbidden,
    Owner
}

/// <summary>
/// Ownership checks shared by the session-runtime endpoints. Mirrors the
/// Doctor/Parent lookup pattern used by GetDoctorChildren / CreateActivity:
/// JWT NameIdentifier → ApplicationUser.Id → Doctor.UserId or Parent.UserId.
/// </summary>
internal static class SessionOwnership
{
    public static async Task<ChildOwnershipStatus> CheckChildOwnershipAsync(
        AppDbContext db,
        string userId,
        int childId,
        CancellationToken cancellationToken)
    {
        var child = await db.Children
            .AsNoTracking()
            .Where(c => c.Id == childId && !c.IsDeleted)
            .Select(c => new { c.DoctorId, c.ParentId })
            .SingleOrDefaultAsync(cancellationToken);

        if (child is null)
        {
            return ChildOwnershipStatus.ChildNotFound;
        }

        bool isOwningDoctor = await db.Doctor
            .AsNoTracking()
            .AnyAsync(
                d => d.UserId == userId && !d.IsDeleted && d.Id == child.DoctorId,
                cancellationToken);

        if (isOwningDoctor)
        {
            return ChildOwnershipStatus.Owner;
        }

        if (child.ParentId is int parentId)
        {
            bool isOwningParent = await db.Parents
                .AsNoTracking()
                .AnyAsync(
                    p => p.UserId == userId && !p.IsDeleted && p.Id == parentId,
                    cancellationToken);

            if (isOwningParent)
            {
                return ChildOwnershipStatus.Owner;
            }
        }

        return ChildOwnershipStatus.Forbidden;
    }

    /// <summary>Loads the session's ChildId, then delegates to the child ownership check.</summary>
    public static async Task<(ChildOwnershipStatus Status, Session? Session)> CheckSessionOwnershipAsync(
        AppDbContext db,
        string userId,
        int sessionId,
        CancellationToken cancellationToken)
    {
        Session? session = await db.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsDeleted, cancellationToken);

        if (session is null)
        {
            return (ChildOwnershipStatus.ChildNotFound, null);
        }

        ChildOwnershipStatus status =
            await CheckChildOwnershipAsync(db, userId, session.ChildId, cancellationToken);

        return (status, session);
    }
}
