using NomoAI.API.Domain.Entities;

namespace NomoDoc.Domain.Entities;

public class SessionSummary : BaseEntity<int>
{
    public int SessionId { get; set; }

    public int TotalAttempts { get; set; }

    public int SuccessfulAttempts { get; set; }

    public decimal AverageScore { get; set; }

    public decimal BestScore { get; set; }

    public decimal ImprovementPercentage { get; set; }

    /// <summary>JSON array of strength strings.</summary>
    public string Strengths { get; set; } = "[]";

    /// <summary>JSON array of practice-area strings (parent/doctor facing).</summary>
    public string Weaknesses { get; set; } = "[]";

    public string Recommendations { get; set; } = string.Empty;

    public string AISummary { get; set; } = string.Empty;

    public string? Outcome { get; set; }

    public string? ScoreTrend { get; set; }

    public decimal? FirstOverallScore { get; set; }

    public decimal? FinalOverallScore { get; set; }

    public int NoSpeechAttempts { get; set; }

    public int InterventionCount { get; set; }

    public string? FinalAdaptiveAction { get; set; }

    public bool DoctorReviewRecommended { get; set; }

    public string? DoctorReviewNote { get; set; }

    public double? DurationSeconds { get; set; }

    public string? SummaryGenerationMode { get; set; }

    public string? ModelName { get; set; }

    public bool UsedFallback { get; set; }

    public string? RulesVersion { get; set; }

    public DateTimeOffset? GeneratedAt { get; set; }

    /// <summary>Full AI summary response snapshot for audit/replay.</summary>
    public string? RawResponseJson { get; set; }

    public Session session { get; set; } = null!;
}
