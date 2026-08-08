using System.Text.Json;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Infrastructure.Ai;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary;

internal static class SessionSummaryMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static void ApplyAiResponse(SessionSummary entity, AiSessionSummaryResponse ai)
    {
        AiSessionSummaryMetricsDto? metrics = ai.Metrics;
        AiSessionSummaryWordingDto? wording = ai.Summary;

        entity.TotalAttempts = metrics?.AttemptCount ?? entity.TotalAttempts;
        entity.SuccessfulAttempts = metrics?.SuccessfulAttempts ?? entity.SuccessfulAttempts;
        entity.NoSpeechAttempts = metrics?.NoSpeechAttempts ?? 0;
        entity.InterventionCount = metrics?.InterventionCount ?? 0;
        entity.AverageScore = ToDecimal(metrics?.AverageOverallScore);
        entity.BestScore = ToDecimal(metrics?.BestOverallScore);
        entity.ImprovementPercentage = ToDecimal(metrics?.ScoreImprovement);
        entity.FirstOverallScore = ToNullableDecimal(metrics?.FirstOverallScore);
        entity.FinalOverallScore = ToNullableDecimal(metrics?.FinalOverallScore);
        entity.ScoreTrend = metrics?.ScoreTrend;
        entity.FinalAdaptiveAction = metrics?.FinalAdaptiveAction;
        entity.DurationSeconds = metrics?.DurationSeconds;
        entity.DoctorReviewRecommended = metrics?.DoctorReviewRecommended ?? false;

        entity.AISummary = wording?.ShortSummary ?? string.Empty;
        entity.Strengths = JsonSerializer.Serialize(wording?.Strengths ?? Array.Empty<string>(), JsonOptions);
        entity.Weaknesses = JsonSerializer.Serialize(wording?.PracticeAreas ?? Array.Empty<string>(), JsonOptions);
        entity.Recommendations = wording?.NextSessionSuggestion ?? string.Empty;
        entity.DoctorReviewNote = wording?.DoctorReviewNote;

        entity.Outcome = ai.Outcome;
        entity.SummaryGenerationMode = ai.SummaryGenerationMode;
        entity.ModelName = ai.Model;
        entity.UsedFallback = ai.UsedFallback;
        entity.RulesVersion = ai.RulesVersion;
        entity.GeneratedAt = ai.GeneratedAt ?? DateTimeOffset.UtcNow;
        entity.RawResponseJson = JsonSerializer.Serialize(ai, AiCoreJsonSerializerOptions.Instance);
    }

    public static SessionSummaryDto ToDto(Session session, SessionSummary summary, string childName)
    {
        return new SessionSummaryDto
        {
            SessionId = session.Id,
            ChildId = session.ChildId,
            ChildName = childName,
            SessionTitle = session.SessionTitle,
            ActivityType = session.ActivityType,
            Prompt = session.Prompt,
            SpeechLevel = session.SpeechLevel,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Outcome = summary.Outcome ?? string.Empty,
            ShortSummary = summary.AISummary,
            Strengths = DeserializeList(summary.Strengths),
            PracticeAreas = DeserializeList(summary.Weaknesses),
            NextSessionSuggestion = summary.Recommendations,
            DoctorReviewNote = summary.DoctorReviewNote,
            DoctorReviewRecommended = summary.DoctorReviewRecommended,
            RequiresDoctorReview = session.RequiresDoctorReview,
            IsDoctorReviewed = session.IsDoctorReviewed,
            TotalAttempts = summary.TotalAttempts,
            SuccessfulAttempts = summary.SuccessfulAttempts,
            NoSpeechAttempts = summary.NoSpeechAttempts,
            InterventionCount = summary.InterventionCount,
            AverageScore = summary.AverageScore,
            BestScore = summary.BestScore,
            ImprovementPercentage = summary.ImprovementPercentage,
            FirstOverallScore = summary.FirstOverallScore,
            FinalOverallScore = summary.FinalOverallScore,
            ScoreTrend = summary.ScoreTrend,
            FinalAdaptiveAction = summary.FinalAdaptiveAction,
            DurationSeconds = summary.DurationSeconds,
            SummaryGenerationMode = summary.SummaryGenerationMode,
            ModelName = summary.ModelName,
            UsedFallback = summary.UsedFallback,
            GeneratedAt = summary.GeneratedAt
        };
    }

    public static DoctorSessionSummaryResponse ToDoctorResponse(SessionSummaryDto dto) => new()
    {
        SessionId = dto.SessionId,
        ChildId = dto.ChildId,
        ChildName = dto.ChildName,
        SessionTitle = dto.SessionTitle,
        ActivityType = dto.ActivityType,
        Prompt = dto.Prompt,
        SpeechLevel = dto.SpeechLevel,
        StartedAt = dto.StartedAt,
        EndedAt = dto.EndedAt,
        Outcome = dto.Outcome,
        ShortSummary = dto.ShortSummary,
        Strengths = dto.Strengths,
        PracticeAreas = dto.PracticeAreas,
        NextSessionSuggestion = dto.NextSessionSuggestion,
        DoctorReviewNote = dto.DoctorReviewNote,
        DoctorReviewRecommended = dto.DoctorReviewRecommended,
        RequiresDoctorReview = dto.RequiresDoctorReview,
        IsDoctorReviewed = dto.IsDoctorReviewed,
        Metrics = new SessionSummaryMetricsDto
        {
            TotalAttempts = dto.TotalAttempts,
            SuccessfulAttempts = dto.SuccessfulAttempts,
            NoSpeechAttempts = dto.NoSpeechAttempts,
            InterventionCount = dto.InterventionCount,
            AverageScore = dto.AverageScore,
            BestScore = dto.BestScore,
            ImprovementPercentage = dto.ImprovementPercentage,
            FirstOverallScore = dto.FirstOverallScore,
            FinalOverallScore = dto.FinalOverallScore,
            ScoreTrend = dto.ScoreTrend,
            FinalAdaptiveAction = dto.FinalAdaptiveAction,
            DurationSeconds = dto.DurationSeconds
        },
        SummaryGenerationMode = dto.SummaryGenerationMode,
        ModelName = dto.ModelName,
        UsedFallback = dto.UsedFallback,
        GeneratedAt = dto.GeneratedAt
    };

    public static ParentSessionSummaryResponse ToParentResponse(SessionSummaryDto dto)
    {
        string practiceTip = dto.PracticeAreas.Count > 0
            ? dto.PracticeAreas[0]
            : "استمروا في التمرين بهدوء وتشجيع في البيت.";

        bool followUp = dto.DoctorReviewRecommended || dto.RequiresDoctorReview;

        return new ParentSessionSummaryResponse
        {
            SessionId = dto.SessionId,
            ChildId = dto.ChildId,
            ChildName = dto.ChildName,
            SessionTitle = dto.SessionTitle,
            ActivityLabel = MapActivityLabel(dto.ActivityType, dto.Prompt),
            EndedAt = dto.EndedAt,
            FriendlyOutcome = MapFriendlyOutcome(dto.Outcome),
            ShortSummary = dto.ShortSummary,
            Strengths = dto.Strengths.Take(3).ToArray(),
            PracticeTip = practiceTip,
            NextSessionSuggestion = dto.NextSessionSuggestion,
            SuggestDoctorFollowUp = followUp,
            FollowUpMessage = followUp
                ? "يفضّل متابعة بسيطة مع أخصائي التخاطب عند الحاجة."
                : null,
            TotalAttempts = dto.TotalAttempts,
            SuccessfulAttempts = dto.SuccessfulAttempts,
            GeneratedAt = dto.GeneratedAt
        };
    }

    private static string MapFriendlyOutcome(string outcome) => outcome switch
    {
        "completed_successfully" => "جلسة ناجحة ومشجعة",
        "completed_with_progress" => "فيه تحسن لطيف خلال الجلسة",
        "completed_needs_practice" => "جلسة جيدة وتحتاج تمرين بسيط إضافي",
        "ended_max_attempts" => "انتهت محاولات اليوم بهدوء",
        "ended_no_speech" => "ما قدرنا نسمع صوت واضح اليوم",
        "doctor_review_recommended" => "جلسة تحتاج متابعة مع الأخصائي",
        _ => "ملخص الجلسة جاهز"
    };

    private static string MapActivityLabel(string? activityType, string? prompt)
    {
        string target = string.IsNullOrWhiteSpace(prompt) ? string.Empty : $" — {prompt}";
        return activityType switch
        {
            "character" => $"تدريب حرف{target}",
            "word" => $"تدريب كلمة{target}",
            "sentence" => $"تدريب جملة{target}",
            "conversation" => $"محادثة{target}",
            _ => string.IsNullOrWhiteSpace(prompt) ? "جلسة تخاطب" : prompt
        };
    }

    private static IReadOnlyList<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            // Legacy plain text — treat as one item.
            return [json];
        }
    }

    private static decimal ToDecimal(double? value) =>
        value is null ? 0m : Convert.ToDecimal(Math.Round(value.Value, 2));

    private static decimal? ToNullableDecimal(double? value) =>
        value is null ? null : Convert.ToDecimal(Math.Round(value.Value, 2));
}
