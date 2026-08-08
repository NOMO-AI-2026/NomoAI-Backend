using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Sessions.Summary;

namespace NomoAI.API.Tests;

public class SessionSummaryMapperTests
{
    [Fact]
    public void Parent_Response_Hides_Clinical_Doctor_Note_And_Raw_Scores()
    {
        var dto = new SessionSummaryDto
        {
            SessionId = 10,
            ChildId = 3,
            ChildName = "أحمد",
            SessionTitle = "تدريب كلمة بابا",
            ActivityType = "word",
            Prompt = "بابا",
            Outcome = "doctor_review_recommended",
            OutcomeLabel = "تحتاج مراجعة أخصائي التخاطب",
            ShortSummary = "أظهر أحمد مشاركة هادئة خلال الجلسة.",
            Strengths = ["حاول بتشجيع", "انتباه جيد", "نطق أوضح في النهاية", "زيادة إضافية"],
            PracticeAreas = ["كرر كلمة بابا ببطء في البيت"],
            NextSessionSuggestion = "جلسة قصيرة أخرى على نفس الهدف",
            DoctorReviewNote = "Internal clinical note — must not leak to parent",
            DoctorReviewRecommended = true,
            RequiresDoctorReview = true,
            TotalAttempts = 4,
            SuccessfulAttempts = 2,
            AverageScore = 72,
            BestScore = 80
        };

        ParentSessionSummaryResponse parent = SessionSummaryMapper.ToParentResponse(dto);

        Assert.Equal("جلسة تحتاج متابعة مع الأخصائي", parent.FriendlyOutcome);
        Assert.DoesNotContain("Internal clinical", parent.ShortSummary);
        Assert.Null(parent.GetType().GetProperty("DoctorReviewNote"));
        Assert.True(parent.SuggestDoctorFollowUp);
        Assert.Contains("أخصائي التخاطب", parent.FollowUpMessage);
        Assert.Equal(3, parent.Strengths.Count);
        Assert.Equal("كرر كلمة بابا ببطء في البيت", parent.PracticeTip);
        Assert.DoesNotContain(parent.ShortSummary, "72");
    }

    [Fact]
    public void Doctor_Response_Keeps_Metrics_And_Review_Note()
    {
        var dto = new SessionSummaryDto
        {
            SessionId = 10,
            ChildId = 3,
            ChildName = "أحمد",
            SessionTitle = "تدريب",
            Outcome = "completed_with_progress",
            OutcomeLabel = "اكتملت مع تحسن ملحوظ في الدرجات",
            ShortSummary = "تحسن ملحوظ",
            Strengths = ["وضوح أفضل"],
            PracticeAreas = ["الطول الزمني"],
            NextSessionSuggestion = "كرر الهدف",
            DoctorReviewNote = "راجع نطق المقطع الأول",
            DoctorReviewRecommended = true,
            TotalAttempts = 5,
            SuccessfulAttempts = 3,
            AverageScore = 68,
            BestScore = 77,
            ScoreTrend = "improving"
        };

        DoctorSessionSummaryResponse doctor = SessionSummaryMapper.ToDoctorResponse(dto);

        Assert.Equal("راجع نطق المقطع الأول", doctor.DoctorReviewNote);
        Assert.Equal(68, doctor.Metrics.AverageScore);
        Assert.Equal("improving", doctor.Metrics.ScoreTrend);
        Assert.True(doctor.DoctorReviewRecommended);
        Assert.Equal("اكتملت مع تحسن ملحوظ في الدرجات", doctor.OutcomeLabel);
        Assert.DoesNotContain("completed_with_progress", doctor.OutcomeLabel);
    }
}

public class SessionSummaryAnalyticsTests
{
    [Fact]
    public void Compute_Uses_Total_Attempts_And_Arabic_Analytical_Outcome()
    {
        var session = new Session
        {
            Id = 1,
            SessionTitle = "تدريب بابا",
            Prompt = "بابا",
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            EndedAt = DateTime.UtcNow
        };

        var attempts = new List<SessionSummaryAnalytics.AttemptSignal>
        {
            new(1, 10, false, "scored", "retry_same"),
            new(2, 12, false, "scored", "retry_same"),
            new(3, 14, false, "scored", "retry_with_hint"),
            new(4, 13, false, "scored", "retry_same"),
            new(5, 15, false, "scored", "retry_same"),
            new(6, 16, false, "scored", "retry_same"),
            new(7, 14, false, "scored", "retry_same"),
            new(8, 16, false, "scored", "end_attempts"),
        };

        SessionSummaryAnalytics.ComputedAnalytics analytics =
            SessionSummaryAnalytics.Compute(session, attempts);

        Assert.Equal(8, analytics.TotalAttempts);
        Assert.Equal(0, analytics.SuccessfulAttempts);
        Assert.Equal(8, analytics.ScoredAttempts);
        Assert.Equal("ended_max_attempts", analytics.Outcome);
        Assert.Contains("عدد المحاولات 8", analytics.ShortSummary);
        Assert.Contains(
            "انتهت ببلوغ الحد الأقصى",
            SessionSummaryAnalytics.MapDoctorOutcomeLabel(analytics.Outcome));
        Assert.True(analytics.BestScore >= 15);
    }

    [Fact]
    public void Compute_Counts_Matched_Attempts_As_Successful()
    {
        var session = new Session
        {
            Id = 2,
            SessionTitle = "تدريب",
            Prompt = "ماما",
            StartedAt = DateTime.UtcNow.AddMinutes(-3),
            EndedAt = DateTime.UtcNow
        };

        var attempts = new List<SessionSummaryAnalytics.AttemptSignal>
        {
            new(1, 40, false, "scored", "retry_same"),
            new(2, 85, true, "scored", "advance"),
            new(3, 88, true, "scored", "advance"),
        };

        SessionSummaryAnalytics.ComputedAnalytics analytics =
            SessionSummaryAnalytics.Compute(session, attempts);

        Assert.Equal(3, analytics.TotalAttempts);
        Assert.Equal(2, analytics.SuccessfulAttempts);
        Assert.Equal("completed_successfully", analytics.Outcome);
    }
}
