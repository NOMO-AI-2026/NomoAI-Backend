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
        Assert.Equal(3, parent.Strengths.Count); // capped for parent
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
    }
}
