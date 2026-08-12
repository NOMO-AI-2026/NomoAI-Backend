using Microsoft.EntityFrameworkCore;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime;
using NomoAI.API.Persistence;

namespace NomoAI.API.Tests;

/// <summary>
/// QA-style coverage for wallet debit after session completion (actual duration billing).
/// </summary>
public class DoctorSessionCreditDebiterIntegrationTests
{
    [Fact]
    public async Task Completed_Short_Session_Debits_Actual_Minutes_Not_Estimate()
    {
        await using AppDbContext db = CreateDb();
        SeedContext seed = await SeedAsync(db, availableMinutes: 120, estimatedMinutes: 30);

        seed.Session.Status = SessionStatus.Completed;
        seed.Session.StartedAt = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);
        seed.Session.EndedAt = seed.Session.StartedAt.Value.AddMinutes(2).AddSeconds(10); // -> 3 billable minutes
        await db.SaveChangesAsync();

        await DoctorSessionCreditDebiter.TryDebitForCompletedSessionAsync(
            db, seed.Session, CancellationToken.None);
        await db.SaveChangesAsync();

        DoctorCreditWallet wallet = await db.DoctorCreditWallets.SingleAsync(w => w.DoctorId == seed.DoctorId);
        DoctorTransaction tx = await db.DoctorTransactions.SingleAsync(t => t.SessionId == seed.Session.Id);

        Assert.Equal(117, wallet.AvailableMinutes); // 120 - 3
        Assert.Equal(TransactionType.SessionUsage, tx.Type);
        Assert.Equal(3, tx.Minutes);
        Assert.Equal(117, tx.BalanceAfter);
        Assert.Equal(seed.DoctorId, tx.DoctorId);
    }

    [Fact]
    public async Task Quick_Close_Session_Debits_Only_One_Minute()
    {
        await using AppDbContext db = CreateDb();
        SeedContext seed = await SeedAsync(db, availableMinutes: 50, estimatedMinutes: 45);

        DateTime start = new(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);
        seed.Session.Status = SessionStatus.Completed;
        seed.Session.StartedAt = start;
        seed.Session.EndedAt = start.AddSeconds(25);
        await db.SaveChangesAsync();

        await DoctorSessionCreditDebiter.TryDebitForCompletedSessionAsync(
            db, seed.Session, CancellationToken.None);
        await db.SaveChangesAsync();

        DoctorCreditWallet wallet = await db.DoctorCreditWallets.SingleAsync(w => w.DoctorId == seed.DoctorId);
        DoctorTransaction tx = await db.DoctorTransactions.SingleAsync(t => t.SessionId == seed.Session.Id);

        Assert.Equal(49, wallet.AvailableMinutes);
        Assert.Equal(1, tx.Minutes);
    }

    [Fact]
    public async Task Long_Abandoned_Session_Is_Capped_By_EstimatedDurationMinutes()
    {
        await using AppDbContext db = CreateDb();
        SeedContext seed = await SeedAsync(db, availableMinutes: 200, estimatedMinutes: 30);

        DateTime start = new(2026, 8, 12, 8, 0, 0, DateTimeKind.Utc);
        seed.Session.Status = SessionStatus.Completed;
        seed.Session.StartedAt = start;
        seed.Session.EndedAt = start.AddHours(5); // actual 300 minutes, estimate 30
        await db.SaveChangesAsync();

        await DoctorSessionCreditDebiter.TryDebitForCompletedSessionAsync(
            db, seed.Session, CancellationToken.None);
        await db.SaveChangesAsync();

        DoctorCreditWallet wallet = await db.DoctorCreditWallets.SingleAsync();
        DoctorTransaction tx = await db.DoctorTransactions.SingleAsync();

        Assert.Equal(170, wallet.AvailableMinutes); // 200 - 30
        Assert.Equal(30, tx.Minutes);
    }

    [Fact]
    public async Task Debit_Is_Idempotent_For_Same_Session()
    {
        await using AppDbContext db = CreateDb();
        SeedContext seed = await SeedAsync(db, availableMinutes: 100, estimatedMinutes: 20);

        DateTime start = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);
        seed.Session.Status = SessionStatus.Completed;
        seed.Session.StartedAt = start;
        seed.Session.EndedAt = start.AddMinutes(10);
        await db.SaveChangesAsync();

        await DoctorSessionCreditDebiter.TryDebitForCompletedSessionAsync(
            db, seed.Session, CancellationToken.None);
        await db.SaveChangesAsync();

        await DoctorSessionCreditDebiter.TryDebitForCompletedSessionAsync(
            db, seed.Session, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.DoctorTransactions.CountAsync(t => t.SessionId == seed.Session.Id));
        Assert.Equal(90, (await db.DoctorCreditWallets.SingleAsync()).AvailableMinutes);
    }

    [Fact]
    public async Task Insufficient_Balance_Floors_At_Zero_And_Debits_What_Remains()
    {
        await using AppDbContext db = CreateDb();
        SeedContext seed = await SeedAsync(db, availableMinutes: 2, estimatedMinutes: 30);

        DateTime start = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);
        seed.Session.Status = SessionStatus.Completed;
        seed.Session.StartedAt = start;
        seed.Session.EndedAt = start.AddMinutes(10);
        await db.SaveChangesAsync();

        await DoctorSessionCreditDebiter.TryDebitForCompletedSessionAsync(
            db, seed.Session, CancellationToken.None);
        await db.SaveChangesAsync();

        DoctorCreditWallet wallet = await db.DoctorCreditWallets.SingleAsync();
        DoctorTransaction tx = await db.DoctorTransactions.SingleAsync();

        Assert.Equal(0, wallet.AvailableMinutes);
        Assert.Equal(2, tx.Minutes);
        Assert.Equal(0, tx.BalanceAfter);
    }

    [Fact]
    public async Task InProgress_Session_Does_Not_Debit()
    {
        await using AppDbContext db = CreateDb();
        SeedContext seed = await SeedAsync(db, availableMinutes: 80, estimatedMinutes: 20);

        seed.Session.Status = SessionStatus.InProgress;
        seed.Session.StartedAt = new DateTime(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);
        await db.SaveChangesAsync();

        await DoctorSessionCreditDebiter.TryDebitForCompletedSessionAsync(
            db, seed.Session, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Empty(db.DoctorTransactions);
        Assert.Equal(80, (await db.DoctorCreditWallets.SingleAsync()).AvailableMinutes);
    }

    [Fact]
    public async Task Missing_StartedAt_Falls_Back_To_CreatedAt()
    {
        await using AppDbContext db = CreateDb();
        SeedContext seed = await SeedAsync(db, availableMinutes: 80, estimatedMinutes: 20);

        DateTime created = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);
        seed.Session.Status = SessionStatus.Completed;
        seed.Session.CreatedAt = created;
        seed.Session.StartedAt = null;
        seed.Session.EndedAt = created.AddMinutes(4);
        await db.SaveChangesAsync();

        await DoctorSessionCreditDebiter.TryDebitForCompletedSessionAsync(
            db, seed.Session, CancellationToken.None);
        await db.SaveChangesAsync();

        DoctorTransaction tx = await db.DoctorTransactions.SingleAsync();
        Assert.Equal(4, tx.Minutes);
        Assert.Equal(76, (await db.DoctorCreditWallets.SingleAsync()).AvailableMinutes);
    }

    private static AppDbContext CreateDb()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<SeedContext> SeedAsync(
        AppDbContext db,
        int availableMinutes,
        int estimatedMinutes)
    {
        var level = new SpeechLevel
        {
            Id = 1,
            LevelName = "L1",
            Description = "QA level"
        };
        var doctor = new Doctor
        {
            Id = 10,
            UserId = Guid.NewGuid().ToString(),
            IsApproved = true
        };
        var child = new Children
        {
            Id = 20,
            DoctorId = doctor.Id,
            SpeechLevelId = level.Id,
            FullName = "Test Child",
            DateOfBirth = new DateOnly(2018, 1, 1),
            TherapyStartDate = new DateOnly(2024, 1, 1),
            Age = 8,
            Gender = Gender.Male
        };
        var activity = new Activity
        {
            Id = 30,
            ChildId = child.Id,
            Content = "بابا",
            EstimatedDurationMinutes = estimatedMinutes,
            CanMakeSession = true,
            ActivityTarget = ActivityTargetType.OneWord
        };
        var wallet = new DoctorCreditWallet
        {
            DoctorId = doctor.Id,
            AvailableMinutes = availableMinutes,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var session = new Session
        {
            Id = 40,
            ChildId = child.Id,
            ActivityId = activity.Id,
            SessionTitle = "QA session",
            Status = SessionStatus.InProgress,
            Language = "ar"
        };

        db.SpeechLevels.Add(level);
        db.Doctor.Add(doctor);
        db.Children.Add(child);
        db.Activities.Add(activity);
        db.DoctorCreditWallets.Add(wallet);
        db.Sessions.Add(session);
        await db.SaveChangesAsync();

        return new SeedContext(doctor.Id, session);
    }

    private sealed record SeedContext(int DoctorId, Session Session);
}
