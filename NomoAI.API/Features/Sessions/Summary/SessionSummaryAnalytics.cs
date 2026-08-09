using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Infrastructure.Ai;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary;

/// <summary>
/// Authoritative session analytics from persisted attempts (not soft AI wording).
/// </summary>
internal static class SessionSummaryAnalytics
{
    private const double SuccessScoreThreshold = 80.0;
    private const double ProgressDelta = 8.0;
    private const double TrendDelta = 5.0;

    public sealed record AttemptSignal(
        int AttemptNumber,
        double? OverallScore,
        bool Matched,
        string SpeechOutcome,
        string? AdaptiveAction);

    public sealed record ComputedAnalytics(
        int TotalAttempts,
        int SuccessfulAttempts,
        int ScoredAttempts,
        int NoSpeechAttempts,
        int InterventionCount,
        decimal AverageScore,
        decimal BestScore,
        decimal ImprovementPercentage,
        decimal? FirstOverallScore,
        decimal? FinalOverallScore,
        string ScoreTrend,
        string? FinalAdaptiveAction,
        double? DurationSeconds,
        string Outcome,
        string ShortSummary,
        IReadOnlyList<string> Strengths,
        IReadOnlyList<string> PracticeAreas,
        string NextSessionSuggestion,
        string? DoctorReviewNote,
        bool DoctorReviewRecommended);

    public static async Task<IReadOnlyList<AttemptSignal>> LoadAttemptSignalsAsync(
        AppDbContext db,
        int sessionId,
        CancellationToken cancellationToken)
    {
        var rows = await (
                from a in db.SessionAttempts.AsNoTracking()
                join e in db.AttemptEvaluations.AsNoTracking() on a.Id equals e.AttemptId
                where a.SessionId == sessionId && !a.IsDeleted && !e.IsDeleted
                orderby a.AttemptNumber
                select new { a.AttemptNumber, e })
            .ToListAsync(cancellationToken);

        var signals = new List<AttemptSignal>(rows.Count);
        foreach (var row in rows)
        {
            AiEvaluateAttemptV2Response? snapshot = Deserialize(row.e.EvaluationJson);
            double? overall = snapshot?.Scores?.Overall
                ?? snapshot?.SpeechAnalysis?.Scores?.OverallScore;

            if (overall is null)
            {
                overall = (double)(
                    (row.e.AccuracyScore
                     + row.e.FluencyScore
                     + row.e.PronunciationScore
                     + row.e.CompletenessScore) / 4m);
            }

            overall = NormalizeScore(overall);

            bool matched = row.e.IsSuccessful
                || row.e.Matched == true
                || snapshot?.Scores?.Matched == true
                || snapshot?.SpeechAnalysis?.Scores?.Matched == true
                || (overall is >= SuccessScoreThreshold);

            string speechOutcome = row.e.SpeechOutcome
                ?? snapshot?.SpeechOutcome
                ?? "scored";

            signals.Add(new AttemptSignal(
                row.AttemptNumber,
                overall,
                matched,
                speechOutcome,
                row.e.AdaptiveAction ?? snapshot?.AdaptiveDecision.Action));
        }

        return signals;
    }

    public static ComputedAnalytics Compute(
        Session session,
        IReadOnlyList<AttemptSignal> attempts)
    {
        int total = attempts.Count;
        int noSpeech = attempts.Count(a => IsNoSpeech(a.SpeechOutcome));
        int scored = total - noSpeech;
        int successful = attempts.Count(a => a.Matched);
        int interventions = attempts.Count(a =>
            a.AdaptiveAction is "retry_with_hint" or "simplify");

        List<double> overalls = attempts
            .Where(a => a.OverallScore is not null)
            .Select(a => a.OverallScore!.Value)
            .ToList();

        double? first = overalls.Count > 0 ? overalls[0] : null;
        double? final = overalls.Count > 0 ? overalls[^1] : null;
        double? best = overalls.Count > 0 ? overalls.Max() : null;
        double? average = overalls.Count > 0 ? overalls.Average() : null;
        double improvement = first is not null && final is not null
            ? Math.Round(final.Value - first.Value, 2)
            : 0;

        string trend = ComputeTrend(overalls);
        string? finalAction = attempts.Count > 0 ? attempts[^1].AdaptiveAction : null;
        bool doctorReview = session.RequiresDoctorReview
            || attempts.Any(a => a.AdaptiveAction == "recommend_doctor_review");

        double? duration = null;
        if (session.StartedAt is DateTime started)
        {
            DateTime ended = session.EndedAt ?? DateTime.UtcNow;
            duration = Math.Max(0, (ended - started).TotalSeconds);
        }

        string outcome = SelectOutcome(
            total,
            noSpeech,
            successful,
            best,
            final,
            improvement,
            trend,
            finalAction,
            doctorReview);

        (string summary, IReadOnlyList<string> strengths, IReadOnlyList<string> practice, string next, string? note)
            = BuildAnalyticalWording(
                session,
                total,
                successful,
                scored,
                noSpeech,
                average,
                best,
                first,
                final,
                improvement,
                trend,
                outcome,
                doctorReview);

        return new ComputedAnalytics(
            TotalAttempts: total,
            SuccessfulAttempts: successful,
            ScoredAttempts: scored,
            NoSpeechAttempts: noSpeech,
            InterventionCount: interventions,
            AverageScore: ToDecimal(average),
            BestScore: ToDecimal(best),
            ImprovementPercentage: ToDecimal(improvement),
            FirstOverallScore: ToNullableDecimal(first),
            FinalOverallScore: ToNullableDecimal(final),
            ScoreTrend: trend,
            FinalAdaptiveAction: finalAction,
            DurationSeconds: duration,
            Outcome: outcome,
            ShortSummary: summary,
            Strengths: strengths,
            PracticeAreas: practice,
            NextSessionSuggestion: next,
            DoctorReviewNote: note,
            DoctorReviewRecommended: doctorReview);
    }

    public static void ApplyToEntity(SessionSummary entity, ComputedAnalytics analytics)
    {
        entity.TotalAttempts = analytics.TotalAttempts;
        entity.SuccessfulAttempts = analytics.SuccessfulAttempts;
        entity.NoSpeechAttempts = analytics.NoSpeechAttempts;
        entity.InterventionCount = analytics.InterventionCount;
        entity.AverageScore = analytics.AverageScore;
        entity.BestScore = analytics.BestScore;
        entity.ImprovementPercentage = analytics.ImprovementPercentage;
        entity.FirstOverallScore = analytics.FirstOverallScore;
        entity.FinalOverallScore = analytics.FinalOverallScore;
        entity.ScoreTrend = analytics.ScoreTrend;
        entity.FinalAdaptiveAction = analytics.FinalAdaptiveAction;
        entity.DurationSeconds = analytics.DurationSeconds;
        entity.DoctorReviewRecommended = analytics.DoctorReviewRecommended;
        entity.Outcome = analytics.Outcome;
        entity.AISummary = analytics.ShortSummary;
        entity.Strengths = JsonSerializer.Serialize(analytics.Strengths);
        entity.Weaknesses = JsonSerializer.Serialize(analytics.PracticeAreas);
        entity.Recommendations = analytics.NextSessionSuggestion;
        entity.DoctorReviewNote = analytics.DoctorReviewNote;
        entity.SummaryGenerationMode = "db_analytics";
        entity.UsedFallback = true;
        entity.GeneratedAt = DateTimeOffset.UtcNow;
    }

    public static string MapDoctorOutcomeLabel(string outcome) => outcome switch
    {
        "completed_successfully" => "اكتملت بنجاح — الهدف تحقق في محاولات الجلسة",
        "completed_with_progress" => "اكتملت مع تحسن ملحوظ في الدرجات",
        "completed_needs_practice" => "اكتملت وتحتاج مزيدًا من التدريب على نفس الهدف",
        "ended_max_attempts" => "انتهت ببلوغ الحد الأقصى للمحاولات دون تحقق كامل للهدف",
        "ended_no_speech" => "انتهت مع ضعف في رصد الكلام الواضح",
        "doctor_review_recommended" => "تحتاج مراجعة أخصائي التخاطب",
        "inconclusive" => "البيانات غير كافية لاستنتاج سريري واضح",
        _ => "نتيجة الجلسة جاهزة للتحليل"
    };

    private static string SelectOutcome(
        int total,
        int noSpeech,
        int successful,
        double? best,
        double? final,
        double improvement,
        string trend,
        string? finalAction,
        bool doctorReview)
    {
        if (doctorReview)
        {
            return "doctor_review_recommended";
        }

        if (total > 0 && (noSpeech >= 2 || noSpeech >= Math.Ceiling(total * 0.67)))
        {
            return "ended_no_speech";
        }

        if (successful > 0 || (best is >= SuccessScoreThreshold) || (final is >= SuccessScoreThreshold))
        {
            return "completed_successfully";
        }

        if (trend == "improving" && improvement >= ProgressDelta)
        {
            return "completed_with_progress";
        }

        if (finalAction is "end_attempts" || (total >= 5 && finalAction is "end_attempts" or "recommend_doctor_review"))
        {
            return "ended_max_attempts";
        }

        if (total == 0)
        {
            return "inconclusive";
        }

        return "completed_needs_practice";
    }

    private static (string, IReadOnlyList<string>, IReadOnlyList<string>, string, string?) BuildAnalyticalWording(
        Session session,
        int total,
        int successful,
        int scored,
        int noSpeech,
        double? average,
        double? best,
        double? first,
        double? final,
        double improvement,
        string trend,
        string outcome,
        bool doctorReview)
    {
        string target = string.IsNullOrWhiteSpace(session.Prompt)
            ? session.SessionTitle
            : session.Prompt!;

        string avgText = average is null ? "غير متاح" : $"{Math.Round(average.Value)}%";
        string bestText = best is null ? "غير متاح" : $"{Math.Round(best.Value)}%";
        string firstText = first is null ? "—" : $"{Math.Round(first.Value)}%";
        string finalText = final is null ? "—" : $"{Math.Round(final.Value)}%";
        string trendAr = trend switch
        {
            "improving" => "تحسن",
            "declining" => "تراجع",
            "mixed" => "متفاوت",
            "stable" => "ثابت",
            _ => "غير متاح"
        };

        string summary =
            $"تحليل الجلسة على الهدف «{target}»: عدد المحاولات {total}، منها {scored} بمحاولة صوتية مسجّلة و{noSpeech} بدون كلام واضح. " +
            $"المحاولات المطابقة للهدف {successful}. متوسط الدرجة {avgText}، أفضل درجة {bestText}، من {firstText} إلى {finalText} (تغير {improvement:+0;-0;0} نقطة)، والاتجاه {trendAr}. " +
            MapDoctorOutcomeLabel(outcome) + ".";

        var strengths = new List<string>();
        if (total > 0)
        {
            strengths.Add($"أكمل الطفل {total} محاولة/محاولات مسجّلة في الجلسة.");
        }

        if (scored > 0)
        {
            strengths.Add($"وُجدت {scored} محاولة صوتية قابلة للتقييم.");
        }

        if (successful > 0)
        {
            strengths.Add($"تحقّقت مطابقة الهدف في {successful} محاولة.");
        }

        if (improvement > 0)
        {
            strengths.Add($"تحسن الدرجة بمقدار {improvement:0} نقطة بين أول وآخر محاولة.");
        }

        if (strengths.Count == 0)
        {
            strengths.Add("الجلسة مسجّلة ويمكن مراجعتها سريريًا.");
        }

        var practice = new List<string>();
        if (successful == 0 && scored > 0)
        {
            practice.Add($"إعادة تدريب قصير على «{target}» مع نموذج نطق أوضح.");
        }

        if (noSpeech > 0)
        {
            practice.Add("تشجيع إنتاج صوت قصير في بداية كل محاولة لتقليل محاولات بدون كلام.");
        }

        if (average is < 50)
        {
            practice.Add("تخفيض صعوبة الهدف مؤقتًا أو تقسيمه لمقاطع أصغر.");
        }

        if (practice.Count == 0)
        {
            practice.Add("الاستمرار على نفس الهدف مع تتبع الدرجات عبر الجلسات.");
        }

        string next = outcome switch
        {
            "completed_successfully" => $"يمكن الانتقال لهدف مجاور أو تثبيت «{target}» بجلسة قصيرة.",
            "completed_with_progress" => $"متابعة «{target}» مع تلميح خفيف في الجلسة القادمة.",
            "ended_max_attempts" => $"مراجعة الخطة قبل إعادة «{target}» بعدد محاولات أقل وضغط أقل.",
            "ended_no_speech" => $"إعادة الجلسة مع دعم بصري بسيط لنفس الهدف «{target}».",
            "doctor_review_recommended" => "انتظار توجيه الأخصائي قبل تغيير مستوى الكلام أو الهدف.",
            _ => $"إعادة تدريب موجّه على «{target}» مع قياس المطابقة والدرجة."
        };

        string? note = doctorReview
            ? $"يُفضّل مراجعة الأخصائي: {total} محاولات، مطابقة={successful}، متوسط={avgText}، أفضل={bestText}."
            : null;

        return (summary, strengths, practice, next, note);
    }

    private static string ComputeTrend(IReadOnlyList<double> overalls)
    {
        if (overalls.Count < 2)
        {
            return "not_available";
        }

        int ups = 0;
        int downs = 0;
        for (int i = 1; i < overalls.Count; i++)
        {
            double change = overalls[i] - overalls[i - 1];
            if (change >= TrendDelta)
            {
                ups++;
            }
            else if (change <= -TrendDelta)
            {
                downs++;
            }
        }

        if (ups > 0 && downs > 0)
        {
            return "mixed";
        }

        if (ups > 0 && downs == 0 && overalls[^1] - overalls[0] >= TrendDelta)
        {
            return "improving";
        }

        if (downs > 0 && ups == 0 && overalls[0] - overalls[^1] >= TrendDelta)
        {
            return "declining";
        }

        return "stable";
    }

    private static bool IsNoSpeech(string speechOutcome) =>
        speechOutcome.Trim().ToLowerInvariant() is "no_speech" or "no_speech_detected" or "empty_transcription";

    /// <summary>If scores look like 0–1 ratios, lift them to 0–100.</summary>
    private static double? NormalizeScore(double? score)
    {
        if (score is null)
        {
            return null;
        }

        double value = score.Value;
        if (value is >= 0 and <= 1.5)
        {
            value *= 100;
        }

        return Math.Clamp(Math.Round(value, 2), 0, 100);
    }

    private static AiEvaluateAttemptV2Response? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiEvaluateAttemptV2Response>(
                json,
                AiCoreJsonSerializerOptions.Instance);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static decimal ToDecimal(double? value) =>
        value is null ? 0m : Convert.ToDecimal(Math.Round(value.Value, 2));

    private static decimal? ToNullableDecimal(double? value) =>
        value is null ? null : Convert.ToDecimal(Math.Round(value.Value, 2));
}
