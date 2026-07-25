using System.Text.Json.Serialization;

namespace NomoAI.API.Common.Ai.Contracts;

public sealed class AiSessionSummaryRequest
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("activityId")]
    public required string ActivityId { get; init; }

    [JsonPropertyName("activityType")]
    public required string ActivityType { get; init; }

    [JsonPropertyName("targetValue")]
    public required string TargetValue { get; init; }

    [JsonPropertyName("speechLevel")]
    public required string SpeechLevel { get; init; }

    [JsonPropertyName("age")]
    public required int Age { get; init; }

    [JsonPropertyName("language")]
    public string Language { get; init; } = "ar";

    [JsonPropertyName("attempts")]
    public required IReadOnlyList<AiSummaryAttemptInput> Attempts { get; init; }

    [JsonPropertyName("childId")]
    public string? ChildId { get; init; }

    [JsonPropertyName("startedAt")]
    public DateTimeOffset? StartedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    [JsonPropertyName("maximumAttempts")]
    public int? MaximumAttempts { get; init; }

    [JsonPropertyName("sessionPlanSourceIds")]
    public IReadOnlyList<Guid>? SessionPlanSourceIds { get; init; }

    [JsonPropertyName("additionalContext")]
    public string? AdditionalContext { get; init; }
}

public sealed class AiSummaryAttemptInput
{
    [JsonPropertyName("attemptNumber")]
    public required int AttemptNumber { get; init; }

    [JsonPropertyName("overallScore")]
    public double? OverallScore { get; init; }

    [JsonPropertyName("accuracyScore")]
    public double? AccuracyScore { get; init; }

    [JsonPropertyName("completenessScore")]
    public double? CompletenessScore { get; init; }

    [JsonPropertyName("fluencyScore")]
    public double? FluencyScore { get; init; }

    [JsonPropertyName("pronunciationProxyScore")]
    public double? PronunciationProxyScore { get; init; }

    [JsonPropertyName("speechOutcome")]
    public required string SpeechOutcome { get; init; }

    [JsonPropertyName("adaptiveAction")]
    public required string AdaptiveAction { get; init; }

    [JsonPropertyName("reasonCodes")]
    public IReadOnlyList<string>? ReasonCodes { get; init; }

    [JsonPropertyName("knowledgeSourceIds")]
    public IReadOnlyList<Guid>? KnowledgeSourceIds { get; init; }
}

public sealed class AiSessionSummaryResponse
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("activityId")]
    public string ActivityId { get; init; } = string.Empty;

    [JsonPropertyName("activityType")]
    public string ActivityType { get; init; } = string.Empty;

    [JsonPropertyName("targetValue")]
    public string TargetValue { get; init; } = string.Empty;

    [JsonPropertyName("outcome")]
    public string Outcome { get; init; } = string.Empty;

    [JsonPropertyName("metrics")]
    public AiSessionSummaryMetricsDto? Metrics { get; init; }

    [JsonPropertyName("summary")]
    public AiSessionSummaryWordingDto? Summary { get; init; }

    [JsonPropertyName("knowledgeSourceIds")]
    public IReadOnlyList<Guid> KnowledgeSourceIds { get; init; } = Array.Empty<Guid>();

    [JsonPropertyName("summaryGenerationMode")]
    public string? SummaryGenerationMode { get; init; }

    [JsonPropertyName("model")]
    public string? Model { get; init; }

    [JsonPropertyName("usedFallback")]
    public bool UsedFallback { get; init; }

    [JsonPropertyName("rulesVersion")]
    public string? RulesVersion { get; init; }

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset? GeneratedAt { get; init; }
}

public sealed class AiSessionSummaryMetricsDto
{
    [JsonPropertyName("attemptCount")]
    public int AttemptCount { get; init; }

    [JsonPropertyName("firstOverallScore")]
    public double? FirstOverallScore { get; init; }

    [JsonPropertyName("finalOverallScore")]
    public double? FinalOverallScore { get; init; }

    [JsonPropertyName("bestOverallScore")]
    public double? BestOverallScore { get; init; }

    [JsonPropertyName("averageOverallScore")]
    public double? AverageOverallScore { get; init; }

    [JsonPropertyName("averageAccuracyScore")]
    public double? AverageAccuracyScore { get; init; }

    [JsonPropertyName("averageCompletenessScore")]
    public double? AverageCompletenessScore { get; init; }

    [JsonPropertyName("averageFluencyScore")]
    public double? AverageFluencyScore { get; init; }

    [JsonPropertyName("averagePronunciationProxyScore")]
    public double? AveragePronunciationProxyScore { get; init; }

    [JsonPropertyName("scoreTrend")]
    public string? ScoreTrend { get; init; }

    [JsonPropertyName("scoreImprovement")]
    public double? ScoreImprovement { get; init; }

    [JsonPropertyName("noSpeechAttempts")]
    public int NoSpeechAttempts { get; init; }

    [JsonPropertyName("successfulAttempts")]
    public int SuccessfulAttempts { get; init; }

    [JsonPropertyName("interventionCount")]
    public int InterventionCount { get; init; }

    [JsonPropertyName("finalAdaptiveAction")]
    public string? FinalAdaptiveAction { get; init; }

    [JsonPropertyName("doctorReviewRecommended")]
    public bool DoctorReviewRecommended { get; init; }

    [JsonPropertyName("durationSeconds")]
    public double? DurationSeconds { get; init; }
}

public sealed class AiSessionSummaryWordingDto
{
    [JsonPropertyName("shortSummary")]
    public string ShortSummary { get; init; } = string.Empty;

    [JsonPropertyName("strengths")]
    public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();

    [JsonPropertyName("practiceAreas")]
    public IReadOnlyList<string> PracticeAreas { get; init; } = Array.Empty<string>();

    [JsonPropertyName("nextSessionSuggestion")]
    public string NextSessionSuggestion { get; init; } = string.Empty;

    [JsonPropertyName("doctorReviewNote")]
    public string? DoctorReviewNote { get; init; }
}
