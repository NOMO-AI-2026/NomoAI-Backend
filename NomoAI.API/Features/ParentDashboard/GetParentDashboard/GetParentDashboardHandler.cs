using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Dashboard;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.ParentDashboard.GetParentDashboard;

internal sealed class GetParentDashboardHandler
    : IRequestHandler<
        GetParentDashboardQuery,
        Result<GetParentDashboardResponse>>
{
    private readonly AppDbContext _dbContext;

    public GetParentDashboardHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetParentDashboardResponse>> Handle(
        GetParentDashboardQuery request,
        CancellationToken cancellationToken)
    {
        int parentId = await _dbContext.Parents
            .AsNoTracking()
            .Where(parent =>
                parent.UserId == request.ParentUserId &&
                !parent.IsDeleted)
            .Select(parent => parent.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (parentId == 0)
        {
            return Result.Failure<GetParentDashboardResponse>(
                ParentDashboardErrors.ParentNotFound);
        }

        DateTime utcNow = DateTime.UtcNow;
        DateTime recentWindowStartUtc =
            DashboardRules.GetRecentWindowStartUtc(utcNow);

        var children = await _dbContext.Children
            .AsNoTracking()
            .Where(child =>
                child.ParentId == parentId &&
                !child.IsDeleted)
            .Select(child => new
            {
                child.Id,
                child.FullName,
                child.SpeechLevelId
            })
            .ToListAsync(cancellationToken);

        if (children.Count == 0)
        {
            return Result.Success(
                new GetParentDashboardResponse(
                    GeneratedAtUtc: utcNow,
                    Children: Array.Empty<ParentDashboardChildCardResponse>()));
        }

        List<int> childIds = children
            .Select(child => child.Id)
            .ToList();

        // Assumption (v1): SpeechLevel.Id order represents level rank
        // (same as Doctor Dashboard / DashboardRules.ClassifyLevelChange).
        List<int> speechLevelIds = await _dbContext.SpeechLevels
            .AsNoTracking()
            .Where(level => !level.IsDeleted)
            .OrderBy(level => level.Id)
            .Select(level => level.Id)
            .ToListAsync(cancellationToken);

        int totalSpeechLevels = speechLevelIds.Count;

        Dictionary<int, int> speechLevelRankById = speechLevelIds
            .Select((id, index) => (Id: id, Rank: index + 1))
            .ToDictionary(item => item.Id, item => item.Rank);

        var activities = await _dbContext.Activities
            .AsNoTracking()
            .Where(activity =>
                childIds.Contains(activity.ChildId) &&
                !activity.IsDeleted)
            .Select(activity => new
            {
                activity.Id,
                activity.ChildId,
                activity.Content,
                activity.ActivityTarget,
                activity.CreatedAt,
                IsPending = !activity.Sessions.Any(session =>
                    !session.IsDeleted &&
                    session.Status == SessionStatus.Completed)
            })
            .ToListAsync(cancellationToken);

        Dictionary<int, int> pendingCountByChild = activities
            .Where(activity => activity.IsPending)
            .GroupBy(activity => activity.ChildId)
            .ToDictionary(
                group => group.Key,
                group => group.Count());

        Dictionary<int, (string Content, ActivityTargetType Target)>
            latestActivityByChild = activities
                .GroupBy(activity => activity.ChildId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var latest = group
                            .OrderByDescending(activity =>
                                activity.CreatedAt)
                            .ThenByDescending(activity =>
                                activity.Id)
                            .First();

                        return (
                            Content: latest.Content ?? string.Empty,
                            Target: latest.ActivityTarget);
                    });

        Dictionary<int, int> completedSessionsLast7DaysByChild =
            await _dbContext.Sessions
                .AsNoTracking()
                .Where(session =>
                    childIds.Contains(session.ChildId) &&
                    !session.IsDeleted &&
                    session.Status == SessionStatus.Completed &&
                    (session.EndedAt ?? session.StartedAt) >=
                        recentWindowStartUtc &&
                    (session.EndedAt ?? session.StartedAt) <=
                        utcNow)
                .GroupBy(session => session.ChildId)
                .Select(group => new
                {
                    ChildId = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(
                    item => item.ChildId,
                    item => item.Count,
                    cancellationToken);

        var doctorNotes = await _dbContext.DoctorNotes
            .AsNoTracking()
            .Where(note =>
                childIds.Contains(note.ChildId) &&
                !note.IsDeleted)
            .Select(note => new
            {
                note.Id,
                note.ChildId,
                note.NoteTitle,
                note.NoteContent,
                note.CreatedAt,
                DoctorFullName = note.Doctor.User.Fullname
            })
            .ToListAsync(cancellationToken);

        Dictionary<int, ParentDashboardDoctorNoteResponse>
            latestNoteByChild = doctorNotes
                .Where(note =>
                    !string.IsNullOrWhiteSpace(note.DoctorFullName))
                .GroupBy(note => note.ChildId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var latest = group
                            .OrderByDescending(note => note.CreatedAt)
                            .ThenByDescending(note => note.Id)
                            .First();

                        return new ParentDashboardDoctorNoteResponse(
                            NoteTitle: latest.NoteTitle,
                            NoteContent: latest.NoteContent,
                            DoctorFullName: latest.DoctorFullName.Trim(),
                            CreatedAt: latest.CreatedAt);
                    });

        List<ParentDashboardChildCardResponse> childCards = children
            .OrderBy(child => child.FullName)
            .ThenBy(child => child.Id)
            .Select(child =>
            {
                int completedSpeechLevels =
                    speechLevelRankById.TryGetValue(
                        child.SpeechLevelId,
                        out int rank)
                        ? rank
                        : 0;

                string? currentGoal = null;
                if (latestActivityByChild.TryGetValue(
                        child.Id,
                        out var latestActivity))
                {
                    string trimmedContent =
                        latestActivity.Content.Trim();

                    currentGoal = !string.IsNullOrEmpty(trimmedContent)
                        ? trimmedContent
                        : ToFriendlyActivityTargetLabel(
                            latestActivity.Target);
                }

                pendingCountByChild.TryGetValue(
                    child.Id,
                    out int pendingExercisesCount);

                completedSessionsLast7DaysByChild.TryGetValue(
                    child.Id,
                    out int sessionsCompletedLast7Days);

                latestNoteByChild.TryGetValue(
                    child.Id,
                    out ParentDashboardDoctorNoteResponse? latestNote);

                return new ParentDashboardChildCardResponse(
                    ChildId: child.Id,
                    FullName: child.FullName,
                    Progress: new ParentDashboardProgressResponse(
                        CompletedSpeechLevels: completedSpeechLevels,
                        TotalSpeechLevels: totalSpeechLevels,
                        CurrentGoal: currentGoal),
                    Activity: new ParentDashboardActivityResponse(
                        PendingExercisesCount: pendingExercisesCount,
                        SessionsCompletedLast7Days:
                            sessionsCompletedLast7Days),
                    LatestDoctorNote: latestNote);
            })
            .ToList();

        return Result.Success(
            new GetParentDashboardResponse(
                GeneratedAtUtc: utcNow,
                Children: childCards));
    }

    private static string ToFriendlyActivityTargetLabel(
        ActivityTargetType target) =>
        target switch
        {
            ActivityTargetType.OneCharacter => "One Character",
            ActivityTargetType.OneWord => "One Word",
            ActivityTargetType.Sentence => "Sentence",
            _ => target.ToString()
        };
}
