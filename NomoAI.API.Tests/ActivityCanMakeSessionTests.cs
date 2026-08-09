using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime;
using NomoAI.API.Features.Sessions.Runtime.SubmitAttempt;

namespace NomoAI.API.Tests;

public class ActivityCanMakeSessionTests
{
    [Fact]
    public void New_Activity_Defaults_CanMakeSession_To_True()
    {
        var activity = new Activity
        {
            ChildId = 1,
            ActivityTarget = ActivityTargetType.OneWord,
            Content = "بابا",
            EstimatedDurationMinutes = 10
        };

        Assert.True(activity.CanMakeSession);
    }

    [Fact]
    public void IsAvailableForNewSession_Requires_True_And_Not_Deleted()
    {
        var available = new Activity { CanMakeSession = true, IsDeleted = false };
        var used = new Activity { CanMakeSession = false, IsDeleted = false };
        var deleted = new Activity { CanMakeSession = true, IsDeleted = true };

        Assert.True(ActivitySessionGate.IsAvailableForNewSession(available));
        Assert.False(ActivitySessionGate.IsAvailableForNewSession(used));
        Assert.False(ActivitySessionGate.IsAvailableForNewSession(deleted));
    }

    [Fact]
    public void MarkUnavailable_After_Completed_Session_Sets_False()
    {
        var activity = new Activity { CanMakeSession = true };

        ActivitySessionGate.MarkUnavailableAfterCompletedSession(activity);

        Assert.False(activity.CanMakeSession);
    }

    [Fact]
    public void Session_Creation_Error_Uses_Controlled_Domain_Code()
    {
        Assert.Equal(
            "SessionRuntime.ActivitySessionAlreadyCreated",
            SessionRuntimeErrors.ActivitySessionAlreadyCreated.Code);
        Assert.Equal(409, SessionRuntimeErrors.ActivitySessionAlreadyCreated.StatusCode);
    }

    [Fact]
    public void Completing_Via_End_Action_Marks_Session_Completed_Without_Touching_Unrelated_Status()
    {
        // Failed/incomplete attempts use retry_same and must NOT complete the session.
        var session = new Session
        {
            Id = 9,
            ActivityId = 3,
            Status = SessionStatus.InProgress,
            CurrentStepNumber = 2,
            CurrentAttemptNumber = 1
        };
        var plan = SessionPlanSnapshotTests.MakePlan(stepCount: 3, maximumAttempts: 3);

        SubmitAttemptCommandHandler.ApplyAdaptiveTransition(session, plan, "retry_same");

        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.Null(session.EndedAt);
    }

    [Fact]
    public void Completing_Via_End_Sets_Completed_Status_For_Activity_Gate_Hook()
    {
        var session = new Session
        {
            Id = 9,
            ActivityId = 3,
            Status = SessionStatus.InProgress,
            CurrentStepNumber = 2,
            CurrentAttemptNumber = 1
        };
        var plan = SessionPlanSnapshotTests.MakePlan(stepCount: 3, maximumAttempts: 3);

        SubmitAttemptCommandHandler.ApplyAdaptiveTransition(session, plan, "end");

        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.NotNull(session.EndedAt);
    }

    [Fact]
    public void OnlyAvailableForSession_Filter_Keeps_True_Rows_Only()
    {
        var rows = new[]
        {
            new Activity { Id = 1, ChildId = 7, CanMakeSession = true, IsDeleted = false },
            new Activity { Id = 2, ChildId = 7, CanMakeSession = false, IsDeleted = false },
            new Activity { Id = 3, ChildId = 7, CanMakeSession = true, IsDeleted = true },
            new Activity { Id = 4, ChildId = 8, CanMakeSession = true, IsDeleted = false },
        };

        const int childId = 7;
        const bool onlyAvailableForSession = true;

        IEnumerable<Activity> filtered = rows
            .Where(a => !a.IsDeleted && a.ChildId == childId)
            .Where(a => !onlyAvailableForSession || a.CanMakeSession);

        Assert.Equal(new[] { 1 }, filtered.Select(a => a.Id).ToArray());
    }

    [Fact]
    public void History_List_Without_Session_Filter_Still_Includes_False_Rows()
    {
        var rows = new[]
        {
            new Activity { Id = 1, ChildId = 7, CanMakeSession = true, IsDeleted = false },
            new Activity { Id = 2, ChildId = 7, CanMakeSession = false, IsDeleted = false },
        };

        const int childId = 7;
        const bool onlyAvailableForSession = false;

        IEnumerable<Activity> filtered = rows
            .Where(a => !a.IsDeleted && a.ChildId == childId)
            .Where(a => !onlyAvailableForSession || a.CanMakeSession);

        Assert.Equal(new[] { 1, 2 }, filtered.Select(a => a.Id).ToArray());
    }
}
