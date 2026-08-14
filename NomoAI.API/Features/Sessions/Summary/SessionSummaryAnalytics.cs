using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Infrastructure.Ai;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary;

/// <summary>
/// Authoritative session analytics from persisted attempts (not soft AI wording).
/// Progression-aware: ADVANCE = original/plan target achieved; null scores ≠ zero.
/// </summary>
internal static class SessionSummaryAnalytics
{
    private const double SuccessScoreThreshold = 80.0;
    private const double ProgressDelta = 8.0;
    private const double TrendDelta = 5.0;

    public sealed record AttemptSignal(
        int AttemptId,
        int AttemptNumber,
        double? OverallScore,
        double? AccuracyScore,
        double? CompletenessScore,
        double? FluencyScore,
        double? PronunciationScore,
        bool Matched,
        bool PracticeEqualsOriginal,
        string SpeechOutcome,
        string? AdaptiveAction,
        int HintsUsed,
        int SimplificationLevel,
        string? OriginalTarget,
        string? PracticeTarget);

    public sealed record ComputedAnalytics(
        int TotalAttempts,
        int SuccessfulAttempts,
        int ScoredAttempts,
        int NoSpeechAttempts,
        int InterventionCount,
        int RetryCount,
        int HintCount,
        int SimplificationCount,
        int AccuracySampleCount,
        int CompletenessSampleCount,
        int FluencySampleCount,
        int PronunciationSampleCount,
        bool OriginalTargetAchieved,
        bool PracticeTargetAchieved,
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
                orderby a.AttemptNumber, a.Id
                select new { a.Id, a.AttemptNumber, e })
            .ToListAsync(cancellationToken);

        // Distinct by AttemptId — guard against accidental join duplication.
        rows = rows
            .GroupBy(r => r.Id)
            .Select(g => g.First())
            .OrderBy(r => r.AttemptNumber)
            .ThenBy(r => r.Id)
            .ToList();

        var signals = new List<AttemptSignal>(rows.Count);
        foreach (var row in rows)
        {
            AiEvaluateAttemptV2Response? snapshot = Deserialize(row.e.EvaluationJson);
            AiSessionExecutionDto? execution = snapshot?.SessionExecution;
            AiAdaptiveDecisionDto? decision = snapshot?.AdaptiveDecision;
            string? original = execution?.OriginalTarget ?? decision?.OriginalTarget;
            string? practice = execution?.PracticeTarget ?? decision?.PracticeTarget;
            int hintsUsed = execution?.HintsUsed ?? decision?.HintsUsed ?? 0;
            int simplificationLevel = execution?.SimplificationLevel ?? decision?.SimplificationLevel ?? 0;

            double? accuracy = snapshot?.Scores?.Accuracy
                ?? snapshot?.SpeechAnalysis?.Scores?.AccuracyScore
                ?? (double)row.e.AccuracyScore;

            double? completeness = snapshot?.Scores?.Completeness
                ?? snapshot?.SpeechAnalysis?.Scores?.CompletenessScore
                ?? (double)row.e.CompletenessScore;

            // Prefer JSON nullability; only fall back to DB when the property was present as a number.
            // Missing evidence must stay null — never invent 0 from a coerced column.
            double? fluency = ReadNullableComponent(
                snapshot?.Scores?.Fluency,
                snapshot?.SpeechAnalysis?.Scores?.FluencyScore,
                row.e.FluencyScore,
                snapshotHadScores: snapshot?.Scores is not null || snapshot?.SpeechAnalysis?.Scores is not null);

            double? pronunciation = ReadNullableComponent(
                snapshot?.Scores?.Pronunciation,
                snapshot?.SpeechAnalysis?.Scores?.PronunciationProxyScore,
                row.e.PronunciationScore,
                snapshotHadScores: snapshot?.Scores is not null || snapshot?.SpeechAnalysis?.Scores is not null);

            double? overall = EvidenceAwareScoreMath.ResolveOverall(
                snapshot?.Scores?.Overall ?? snapshot?.SpeechAnalysis?.Scores?.OverallScore,
                accuracy,
                completeness,
                fluency,
                pronunciation);

            string speechOutcome = row.e.SpeechOutcome
                ?? snapshot?.SpeechOutcome
                ?? "scored";

            if (IsNoSpeech(speechOutcome))
            {
                // No-speech must not contribute fabricated overalls to averages.
                overall = null;
            }

            string? originalTarget = original;
            string? practiceTarget = practice;
            bool practiceEqualsOriginal =
                !string.IsNullOrWhiteSpace(originalTarget)
                && !string.IsNullOrWhiteSpace(practiceTarget)
                && string.Equals(
                    originalTarget.Trim(),
                    practiceTarget.Trim(),
                    StringComparison.Ordinal);

            bool matched = row.e.IsSuccessful
                || row.e.Matched == true
                || snapshot?.Scores?.Matched == true
                || snapshot?.SpeechAnalysis?.Scores?.Matched == true;

            signals.Add(new AttemptSignal(
                row.Id,
                row.AttemptNumber,
                overall,
                EvidenceAwareScoreMath.NormalizeScore(accuracy),
                EvidenceAwareScoreMath.NormalizeScore(completeness),
                EvidenceAwareScoreMath.NormalizeScore(fluency),
                EvidenceAwareScoreMath.NormalizeScore(pronunciation),
                matched,
                practiceEqualsOriginal,
                speechOutcome,
                row.e.AdaptiveAction ?? decision?.Action,
                hintsUsed,
                simplificationLevel,
                originalTarget,
                practiceTarget));
        }

        return signals;
    }

    public static ComputedAnalytics Compute(
        Session session,
        IReadOnlyList<AttemptSignal> attempts,
        ILogger? logger = null)
    {
        int total = attempts.Count;
        int distinctIds = attempts.Select(a => a.AttemptId).Distinct().Count();
        int noSpeech = attempts.Count(a => IsNoSpeech(a.SpeechOutcome));
        int scored = total - noSpeech;

        // Original/plan target mastery = ADVANCE (not simplified practice match alone).
        int advances = attempts.Count(a =>
            string.Equals(a.AdaptiveAction, "advance", StringComparison.OrdinalIgnoreCase));
        int successful = advances;

        int retries = attempts.Count(a =>
            a.AdaptiveAction is "retry_same" or "retry_with_hint");
        int hints = attempts.Count(a =>
            a.AdaptiveAction is "retry_with_hint");
        int simplifications = attempts.Count(a =>
            a.AdaptiveAction is "simplify");
        int interventions = hints + simplifications
            + attempts.Count(a => a.AdaptiveAction is "model" or "slow_down");

        bool practiceAchieved = attempts.Any(a =>
            a.Matched && !a.PracticeEqualsOriginal);
        bool originalAchieved = advances > 0
            || attempts.Any(a =>
                a.Matched
                && a.PracticeEqualsOriginal
                && a.AdaptiveAction is "advance" or "end_attempts");

        // Measurable overalls only — exclude no-speech and null overall.
        List<double> overalls = attempts
            .Where(a => a.OverallScore is not null && !IsNoSpeech(a.SpeechOutcome))
            .Select(a => a.OverallScore!.Value)
            .ToList();

        double? first = overalls.Count > 0 ? overalls[0] : null;
        double? final = overalls.Count > 0 ? overalls[^1] : null;
        double? best = overalls.Count > 0 ? overalls.Max() : null;
        double? average = overalls.Count > 0 ? Math.Round(overalls.Average(), 2) : null;
        double improvement = first is not null && final is not null
            ? Math.Round(final.Value - first.Value, 2)
            : 0;

        string trend = ComputeTrend(overalls);
        string? finalAction = attempts.Count > 0 ? attempts[^1].AdaptiveAction : null;
        bool doctorReview = session.RequiresDoctorReview
            || attempts.Any(a => a.AdaptiveAction == "recommend_doctor_review");

        int accuracySamples = attempts.Count(a => a.AccuracyScore is not null && !IsNoSpeech(a.SpeechOutcome));
        int completenessSamples = attempts.Count(a => a.CompletenessScore is not null && !IsNoSpeech(a.SpeechOutcome));
        int fluencySamples = attempts.Count(a => a.FluencyScore is not null);
        int pronunciationSamples = attempts.Count(a => a.PronunciationScore is not null);

        double? duration = null;
        if (session.StartedAt is DateTime started)
        {
            DateTime ended = session.EndedAt ?? DateTime.UtcNow;
            duration = Math.Max(0, (ended - started).TotalSeconds);
        }

        string outcome = SelectOutcome(
            total,
            noSpeech,
            originalAchieved,
            practiceAchieved,
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
                retries,
                hints,
                simplifications,
                originalAchieved,
                practiceAchieved,
                average,
                best,
                first,
                final,
                improvement,
                trend,
                outcome,
                doctorReview,
                fluencySamples,
                pronunciationSamples);

        logger?.LogInformation(
            "SessionSummary diagnostics SessionId={SessionId} AttemptCount={AttemptCount} DistinctAttemptCount={DistinctAttemptCount} MatchedAdvances={Advances} RetryCount={RetryCount} HintCount={HintCount} SimplificationCount={SimplificationCount} AccuracySampleCount={AccuracySampleCount} CompletenessSampleCount={CompletenessSampleCount} FluencySampleCount={FluencySampleCount} PronunciationSampleCount={PronunciationSampleCount} OriginalTargetAchieved={OriginalTargetAchieved}",
            session.Id,
            total,
            distinctIds,
            advances,
            retries,
            hints,
            simplifications,
            accuracySamples,
            completenessSamples,
            fluencySamples,
            pronunciationSamples,
            originalAchieved);

        return new ComputedAnalytics(
            TotalAttempts: total,
            SuccessfulAttempts: successful,
            ScoredAttempts: scored,
            NoSpeechAttempts: noSpeech,
            InterventionCount: interventions,
            RetryCount: retries,
            HintCount: hints,
            SimplificationCount: simplifications,
            AccuracySampleCount: accuracySamples,
            CompletenessSampleCount: completenessSamples,
            FluencySampleCount: fluencySamples,
            PronunciationSampleCount: pronunciationSamples,
            OriginalTargetAchieved: originalAchieved,
            PracticeTargetAchieved: practiceAchieved || originalAchieved,
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
        "completed_successfully" => "اكتملت بنجاح — الهدف الأصلي تحقق",
        "completed_with_progress" => "اكتملت مع تقدّم علاجي (تلميح/تبسيط/إعادة)",
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
        bool originalAchieved,
        bool practiceAchieved,
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

        // ADVANCE / original mastery — not simplified practice match alone.
        if (originalAchieved || finalAction is "advance")
        {
            return "completed_successfully";
        }

        if (practiceAchieved || (trend == "improving" && improvement >= ProgressDelta))
        {
            return "completed_with_progress";
        }

        // High final score on original-scale practice without ADVANCE still counts as success signal.
        if ((best is >= SuccessScoreThreshold || final is >= SuccessScoreThreshold)
            && finalAction is not "recommend_doctor_review")
        {
            return "completed_successfully";
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
        int retries,
        int hints,
        int simplifications,
        bool originalAchieved,
        bool practiceAchieved,
        double? average,
        double? best,
        double? first,
        double? final,
        double improvement,
        string trend,
        string outcome,
        bool doctorReview,
        int fluencySamples,
        int pronunciationSamples)
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

        string progression;
        if (originalAchieved)
        {
            progression = simplifications > 0 || hints > 0 || retries > 0
                ? $"تحقّق الهدف الأصلي بعد دعم علاجي (إعادة={retries}، تلميح={hints}، تبسيط={simplifications})."
                : "تحقّق الهدف الأصلي بشكل مستقل.";
        }
        else if (practiceAchieved)
        {
            progression = "نجح الطفل على هدف مبسّط للممارسة، ولم يُسجَّل بعد تحقق كامل للهدف الأصلي.";
        }
        else
        {
            progression = "لم يُسجَّل تحقق للهدف الأصلي في هذه الجلسة.";
        }

        string evidenceNote = "";
        if (fluencySamples == 0 && pronunciationSamples == 0)
        {
            evidenceNote = " لا تتوفر أدلة طلاقة/نطق صوتية كافية؛ لم تُحتسب كفشل.";
        }
        else if (pronunciationSamples == 0)
        {
            evidenceNote = " لا تتوفر أدلة نطق صوتية كافية؛ لم تُحتسب كفشل.";
        }
        else if (fluencySamples == 0)
        {
            evidenceNote = " لا تتوفر أدلة طلاقة كافية؛ لم تُحتسب كفشل.";
        }

        string summary =
            $"تحليل الجلسة على الهدف «{target}»: عدد المحاولات {total}، منها {scored} بمحاولة صوتية مسجّلة و{noSpeech} بدون كلام واضح. " +
            $"{progression} " +
            $"متوسط الدرجة القابلة للقياس {avgText}، أفضل درجة {bestText}، من {firstText} إلى {finalText} (تغير {improvement:+0;-0;0} نقطة)، والاتجاه {trendAr}.{evidenceNote} " +
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

        if (originalAchieved)
        {
            strengths.Add($"تحقّق الهدف الأصلي ({successful} انتقال ADVANCE).");
        }
        else if (practiceAchieved)
        {
            strengths.Add("نجاح على هدف الممارسة المبسّط (ليس إتقان الهدف الأصلي بعد).");
        }

        if (improvement > 0)
        {
            strengths.Add($"تحسن الدرجة بمقدار {improvement:0} نقطة بين أول وآخر محاولة قابلة للقياس.");
        }

        if (strengths.Count == 0)
        {
            strengths.Add("الجلسة مسجّلة ويمكن مراجعتها سريريًا.");
        }

        var practice = new List<string>();
        if (!originalAchieved && scored > 0)
        {
            practice.Add($"إعادة تدريب قصير على «{target}» مع نموذج نطق أوضح.");
        }

        if (noSpeech > 0)
        {
            practice.Add("تشجيع إنتاج صوت قصير في بداية كل محاولة لتقليل محاولات بدون كلام.");
        }

        // Do not treat missing evidence / early retries as "poor average" when target was achieved.
        if (!originalAchieved && average is < 50)
        {
            practice.Add("تخفيض صعوبة الهدف مؤقتًا أو تقسيمه لمقاطع أصغر.");
        }

        if (practice.Count == 0)
        {
            practice.Add("الاستمرار على نفس الهدف مع تتبع التقدّم عبر الجلسات.");
        }

        string next = outcome switch
        {
            "completed_successfully" => $"يمكن الانتقال لهدف مجاور أو تثبيت «{target}» بجلسة قصيرة.",
            "completed_with_progress" => $"متابعة «{target}» مع تلميح خفيف أو تثبيت بعد التبسيط.",
            "ended_max_attempts" => $"مراجعة الخطة قبل إعادة «{target}» بعدد محاولات أقل وضغط أقل.",
            "ended_no_speech" => $"إعادة الجلسة مع دعم بصري بسيط لنفس الهدف «{target}».",
            "doctor_review_recommended" => "انتظار توجيه الأخصائي قبل تغيير مستوى الكلام أو الهدف.",
            _ => $"إعادة تدريب موجّه على «{target}» مع قياس المطابقة والدرجة."
        };

        string? note = doctorReview
            ? $"يُفضّل مراجعة الأخصائي: {total} محاولات، ADVANCE={successful}، متوسط قابل للقياس={avgText}، أفضل={bestText}."
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

    /// <summary>
    /// Read component scores without converting missing evidence to zero.
    /// When JSON snapshot exists, trust its nullability over legacy DB zeros.
    /// </summary>
    private static double? ReadNullableComponent(
        double? topLevel,
        double? analysisLevel,
        decimal? dbValue,
        bool snapshotHadScores)
    {
        if (topLevel is not null)
        {
            return topLevel;
        }

        if (analysisLevel is not null)
        {
            return analysisLevel;
        }

        // Snapshot present but component omitted/null → no evidence (do not use coerced DB 0).
        if (snapshotHadScores)
        {
            return null;
        }

        // Legacy rows without EvaluationJson: only trust non-zero DB values as weak signal;
        // zero remains ambiguous (could be coerced null) → treat as null for fluency/pronunciation.
        if (dbValue is null || dbValue.Value == 0m)
        {
            return null;
        }

        return (double)dbValue.Value;
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
