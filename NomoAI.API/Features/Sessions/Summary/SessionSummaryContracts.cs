namespace NomoAI.API.Features.Sessions.Summary;

/// <summary>Shared summary projection used before role filtering.</summary>
public sealed class SessionSummaryDto
{
    public int SessionId { get; init; }

    public int ChildId { get; init; }

    public string ChildName { get; init; } = string.Empty;

    public string SessionTitle { get; init; } = string.Empty;

    public string? ActivityType { get; init; }

    public string? Prompt { get; init; }

    public string? SpeechLevel { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? EndedAt { get; init; }

    public string Outcome { get; init; } = string.Empty;

    /// <summary>Arabic clinical interpretation of <see cref="Outcome"/>.</summary>
    public string OutcomeLabel { get; init; } = string.Empty;

    public string ShortSummary { get; init; } = string.Empty;

    public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> PracticeAreas { get; init; } = Array.Empty<string>();

    public string NextSessionSuggestion { get; init; } = string.Empty;

    public string? DoctorReviewNote { get; init; }

    public bool DoctorReviewRecommended { get; init; }

    public bool RequiresDoctorReview { get; init; }

    public bool IsDoctorReviewed { get; init; }

    public int TotalAttempts { get; init; }

    public int SuccessfulAttempts { get; init; }

    public int NoSpeechAttempts { get; init; }

    public int InterventionCount { get; init; }

    public decimal AverageScore { get; init; }

    public decimal BestScore { get; init; }

    public decimal ImprovementPercentage { get; init; }

    public decimal? FirstOverallScore { get; init; }

    public decimal? FinalOverallScore { get; init; }

    public string? ScoreTrend { get; init; }

    public string? FinalAdaptiveAction { get; init; }

    public double? DurationSeconds { get; init; }

    public string? SummaryGenerationMode { get; init; }

    public string? ModelName { get; init; }

    public bool UsedFallback { get; init; }

    public DateTimeOffset? GeneratedAt { get; init; }
}

/// <summary>Full clinical detail for doctors.</summary>
public sealed class DoctorSessionSummaryResponse
{
    public int SessionId { get; init; }

    public int ChildId { get; init; }

    public string ChildName { get; init; } = string.Empty;

    public string SessionTitle { get; init; } = string.Empty;

    public string? ActivityType { get; init; }

    public string? Prompt { get; init; }

    public string? SpeechLevel { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? EndedAt { get; init; }

    public string Outcome { get; init; } = string.Empty;

    /// <summary>Arabic clinical interpretation — never show raw outcome codes in UI.</summary>
    public string OutcomeLabel { get; init; } = string.Empty;

    public string ShortSummary { get; init; } = string.Empty;

    public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> PracticeAreas { get; init; } = Array.Empty<string>();

    public string NextSessionSuggestion { get; init; } = string.Empty;

    public string? DoctorReviewNote { get; init; }

    public bool DoctorReviewRecommended { get; init; }

    public bool RequiresDoctorReview { get; init; }

    public bool IsDoctorReviewed { get; init; }

    public SessionSummaryMetricsDto Metrics { get; init; } = new();

    public string? SummaryGenerationMode { get; init; }

    public string? ModelName { get; init; }

    public bool UsedFallback { get; init; }

    public DateTimeOffset? GeneratedAt { get; init; }
}

/// <summary>Calm, non-clinical summary for parents.</summary>
public sealed class ParentSessionSummaryResponse
{
    public int SessionId { get; init; }

    public int ChildId { get; init; }

    public string ChildName { get; init; } = string.Empty;

    public string SessionTitle { get; init; } = string.Empty;

    public string? ActivityLabel { get; init; }

    public DateTime? EndedAt { get; init; }

    public string FriendlyOutcome { get; init; } = string.Empty;

    public string ShortSummary { get; init; } = string.Empty;

    public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();

    public string PracticeTip { get; init; } = string.Empty;

    public string NextSessionSuggestion { get; init; } = string.Empty;

    public bool SuggestDoctorFollowUp { get; init; }

    public string? FollowUpMessage { get; init; }

    public int TotalAttempts { get; init; }

    public int SuccessfulAttempts { get; init; }

    public DateTimeOffset? GeneratedAt { get; init; }
}

public sealed class SessionSummaryMetricsDto
{
    public int TotalAttempts { get; init; }

    public int SuccessfulAttempts { get; init; }

    public int NoSpeechAttempts { get; init; }

    public int InterventionCount { get; init; }

    public decimal AverageScore { get; init; }

    public decimal BestScore { get; init; }

    public decimal ImprovementPercentage { get; init; }

    public decimal? FirstOverallScore { get; init; }

    public decimal? FinalOverallScore { get; init; }

    public string? ScoreTrend { get; init; }

    public string? FinalAdaptiveAction { get; init; }

    public double? DurationSeconds { get; init; }
}
