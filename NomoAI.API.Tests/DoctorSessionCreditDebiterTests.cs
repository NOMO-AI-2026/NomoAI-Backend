using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime;

namespace NomoAI.API.Tests;

public class DoctorSessionCreditDebiterTests
{
    [Theory]
    [InlineData(120, 30, 90, 30)]
    [InlineData(10, 30, 0, 10)]
    [InlineData(0, 15, 0, 0)]
    [InlineData(45, 0, 45, 0)]
    [InlineData(-5, 10, 0, 0)]
    public void ApplyDebit_Floors_At_Zero_And_Reports_Actual_Debit(
        int available,
        int requested,
        int expectedBalance,
        int expectedDebited)
    {
        (int newBalance, int debited) = DoctorSessionCreditDebiter.ApplyDebit(available, requested);

        Assert.Equal(expectedBalance, newBalance);
        Assert.Equal(expectedDebited, debited);
    }

    [Fact]
    public void CalculateBillableMinutes_Uses_Actual_Elapsed_Time_Rounded_Up()
    {
        DateTime start = new(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

        Assert.Equal(1, DoctorSessionCreditDebiter.CalculateBillableMinutes(start, start.AddSeconds(20)));
        Assert.Equal(1, DoctorSessionCreditDebiter.CalculateBillableMinutes(start, start.AddMinutes(1)));
        Assert.Equal(3, DoctorSessionCreditDebiter.CalculateBillableMinutes(start, start.AddMinutes(2).AddSeconds(1)));
        Assert.Equal(15, DoctorSessionCreditDebiter.CalculateBillableMinutes(start, start.AddMinutes(15)));
    }

    [Fact]
    public void CalculateBillableMinutes_Returns_Zero_When_Timestamps_Missing_Or_Invalid()
    {
        DateTime start = DateTime.UtcNow;

        Assert.Equal(0, DoctorSessionCreditDebiter.CalculateBillableMinutes(null, start));
        Assert.Equal(0, DoctorSessionCreditDebiter.CalculateBillableMinutes(start, null));
        Assert.Equal(0, DoctorSessionCreditDebiter.CalculateBillableMinutes(start, start));
        Assert.Equal(0, DoctorSessionCreditDebiter.CalculateBillableMinutes(start, start.AddMinutes(-1)));
    }

    [Theory]
    [InlineData(3, 30, 3)]   // short session → actual
    [InlineData(45, 30, 30)] // abandoned long session → capped by estimate
    [InlineData(10, 0, 10)]  // no estimate → actual only
    [InlineData(0, 30, 0)]
    public void ResolveBillableMinutes_Prefers_Actual_But_Caps_By_Estimate(
        int actual,
        int estimated,
        int expected)
    {
        Assert.Equal(expected, DoctorSessionCreditDebiter.ResolveBillableMinutes(actual, estimated));
    }

    [Fact]
    public void SessionUsage_Transaction_Type_Is_Defined_For_Ledger()
    {
        Assert.Equal(1, (int)TransactionType.SessionUsage);
        Assert.Equal(0, (int)TransactionType.PlanPurchase);
    }
}
