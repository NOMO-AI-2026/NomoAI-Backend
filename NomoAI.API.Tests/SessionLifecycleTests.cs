using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime;
using NomoAI.API.Features.Sessions.Runtime.GetSessionRuntime;

namespace NomoAI.API.Tests;

public class SessionLifecycleTests
{
    [Fact]
    public void MarkStarted_Sets_InProgress_And_StartedAt()
    {
        var session = new Session { SessionTitle = "s", Status = SessionStatus.Scheduled };

        SessionLifecycle.MarkStarted(session);

        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.NotNull(session.StartedAt);
        Assert.Null(session.EndedAt);
    }

    [Fact]
    public void MarkCompleted_Sets_Completed_And_EndedAt()
    {
        var session = new Session
        {
            SessionTitle = "s",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        SessionLifecycle.MarkCompleted(session);

        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.NotNull(session.EndedAt);
    }

    [Fact]
    public void Scheduled_With_StartedAt_Counts_As_InProgress()
    {
        SessionStatus effective = SessionLifecycle.ResolveEffectiveStatus(
            SessionStatus.Scheduled,
            startedAt: DateTime.UtcNow,
            endedAt: null,
            planJson: null);

        Assert.Equal(SessionStatus.InProgress, effective);
        Assert.True(SessionLifecycle.IsRunnable(new Session
        {
            SessionTitle = "s",
            Status = SessionStatus.Scheduled,
            StartedAt = DateTime.UtcNow,
            PlanJson = "{}"
        }));
    }

    [Fact]
    public void EndedAt_Without_Completed_Status_Counts_As_Completed()
    {
        SessionStatus effective = SessionLifecycle.ResolveEffectiveStatus(
            SessionStatus.InProgress,
            startedAt: DateTime.UtcNow.AddMinutes(-10),
            endedAt: DateTime.UtcNow,
            planJson: "{}");

        Assert.Equal(SessionStatus.Completed, effective);
    }

    [Fact]
    public void Synchronize_Completes_When_Current_Step_Is_Past_The_Plan()
    {
        Session session = new()
        {
            SessionTitle = "s",
            Status = SessionStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            CurrentStepNumber = 99,
            PlanJson = SessionPlanSnapshot.Serialize(SessionPlanSnapshotTests.MakePlan(stepCount: 2))
        };

        bool changed = SessionLifecycle.SynchronizePersistedStatus(
            session,
            SessionPlanSnapshotTests.MakePlan(stepCount: 2));

        Assert.True(changed);
        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.NotNull(session.EndedAt);
    }

    [Fact]
    public void Synchronize_Heals_Scheduled_Started_Session_To_InProgress()
    {
        var plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2);
        Session session = new()
        {
            SessionTitle = "s",
            Status = SessionStatus.Scheduled,
            StartedAt = DateTime.UtcNow,
            CurrentStepNumber = 1,
            PlanJson = SessionPlanSnapshot.Serialize(plan)
        };

        bool changed = SessionLifecycle.SynchronizePersistedStatus(session, plan);

        Assert.True(changed);
        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.Null(session.EndedAt);
    }
}

public class GetSessionRuntimeStatusTests
{
    [Fact]
    public void InProgress_Does_Not_Emit_SessionCompleted()
    {
        var plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2);
        Session session = new()
        {
            Id = 7,
            SessionTitle = "s",
            Status = SessionStatus.InProgress,
            CurrentStepNumber = 1,
            CurrentAttemptNumber = 0,
            PlanJson = SessionPlanSnapshot.Serialize(plan),
            ActivityType = "word",
            Prompt = "بابا"
        };

        var response = GetSessionRuntimeQueryHandler.BuildRuntimeResponse(session);

        Assert.Equal(SessionRuntimeStatus.InProgress, response.Status);
        Assert.NotEqual(SessionRuntimeCommand.SessionCompleted, response.Command);
    }

    [Fact]
    public void Completed_Emits_SessionCompleted()
    {
        var plan = SessionPlanSnapshotTests.MakePlan(stepCount: 2);
        Session session = new()
        {
            Id = 8,
            SessionTitle = "s",
            Status = SessionStatus.Completed,
            CurrentStepNumber = 2,
            CurrentAttemptNumber = 0,
            PlanJson = SessionPlanSnapshot.Serialize(plan),
            ActivityType = "word",
            Prompt = "بابا"
        };

        var response = GetSessionRuntimeQueryHandler.BuildRuntimeResponse(session);

        Assert.Equal(SessionRuntimeStatus.Completed, response.Status);
        Assert.Equal(SessionRuntimeCommand.SessionCompleted, response.Command);
    }

    [Fact]
    public void Scheduled_Does_Not_Pretend_The_Session_Is_Completed()
    {
        Session session = new()
        {
            Id = 9,
            SessionTitle = "s",
            Status = SessionStatus.Scheduled,
            CurrentStepNumber = 1
        };

        var response = GetSessionRuntimeQueryHandler.BuildRuntimeResponse(session);

        Assert.NotEqual(SessionRuntimeCommand.SessionCompleted, response.Command);
        Assert.Equal(SessionRuntimeStatus.InProgress, response.Status);
    }
}
