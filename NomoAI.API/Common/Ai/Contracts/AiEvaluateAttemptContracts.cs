using System.Text.Json.Serialization;

namespace NomoAI.API.Common.Ai.Contracts;

/// <summary>
/// Multipart attempt-evaluation payload. Audio stream ownership remains with the caller;
/// the client disposes multipart content after the request completes.
/// </summary>
public sealed class AiEvaluateAttemptRequest
{
    public required Stream AudioStream { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required string ChildId { get; init; }

    public required string ActivityId { get; init; }

    public required string ActivityType { get; init; }

    public required string TargetValue { get; init; }

    public required string SpeechLevel { get; init; }

    public required int Age { get; init; }

    public required int AttemptNumber { get; init; }

    public string? SessionId { get; init; }

    public int? SessionStepNumber { get; init; }

    public int? MaximumAttempts { get; init; }

    public IReadOnlyList<double>? PreviousAttemptScores { get; init; }

    public string? PreviousDecision { get; init; }

    public int ConsecutiveNoSpeechCount { get; init; }

    public string Language { get; init; } = "ar";

    public string? AdditionalContext { get; init; }
}

public sealed class AiEvaluateAttemptResponse
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; init; }

    [JsonPropertyName("activityId")]
    public string ActivityId { get; init; } = string.Empty;

    [JsonPropertyName("attemptNumber")]
    public int AttemptNumber { get; init; }

    [JsonPropertyName("activityType")]
    public string ActivityType { get; init; } = string.Empty;

    [JsonPropertyName("targetValue")]
    public string TargetValue { get; init; } = string.Empty;

    [JsonPropertyName("speechOutcome")]
    public string SpeechOutcome { get; init; } = string.Empty;

    [JsonPropertyName("speechAnalysis")]
    public AiSpeechAnalysisResultDto? SpeechAnalysis { get; init; }

    [JsonPropertyName("adaptiveDecision")]
    public AiAdaptiveDecisionDto? AdaptiveDecision { get; init; }

    [JsonPropertyName("knowledgeSourceIds")]
    public IReadOnlyList<Guid> KnowledgeSourceIds { get; init; } = Array.Empty<Guid>();

    [JsonPropertyName("avatar")]
    public AiAvatarInstructionDto? Avatar { get; init; }

    [JsonPropertyName("avatarModel")]
    public string? AvatarModel { get; init; }

    [JsonPropertyName("avatarGenerationMode")]
    public string? AvatarGenerationMode { get; init; }

    [JsonPropertyName("usedFallback")]
    public bool UsedFallback { get; init; }

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset? GeneratedAt { get; init; }

    [JsonPropertyName("sessionExecution")]
    public AiSessionExecutionDto? SessionExecution { get; init; }
}

public sealed class AiAdaptiveDecisionDto
{
    [JsonPropertyName("action")]
    public string Action { get; init; } = string.Empty;

    [JsonPropertyName("reasonCodes")]
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();

    [JsonPropertyName("attemptNumber")]
    public int AttemptNumber { get; init; }

    [JsonPropertyName("scoreTrend")]
    public string ScoreTrend { get; init; } = string.Empty;

    [JsonPropertyName("requiresIntervention")]
    public bool RequiresIntervention { get; init; }

    [JsonPropertyName("requiresDoctorReview")]
    public bool RequiresDoctorReview { get; init; }

    [JsonPropertyName("recommendedDifficulty")]
    public string RecommendedDifficulty { get; init; } = string.Empty;

    [JsonPropertyName("rulesVersion")]
    public string? RulesVersion { get; init; }

    [JsonPropertyName("nextAllowedAttempt")]
    public int? NextAllowedAttempt { get; init; }

    [JsonPropertyName("interventionCategories")]
    public IReadOnlyList<string> InterventionCategories { get; init; } = Array.Empty<string>();

    [JsonPropertyName("originalTarget")]
    public string? OriginalTarget { get; init; }

    [JsonPropertyName("practiceTarget")]
    public string? PracticeTarget { get; init; }

    [JsonPropertyName("simplificationLevel")]
    public int SimplificationLevel { get; init; }

    [JsonPropertyName("sessionPhase")]
    public string? SessionPhase { get; init; }

    [JsonPropertyName("hintsUsed")]
    public int HintsUsed { get; init; }
}

public sealed class AiSpeechAnalysisResultDto
{
    [JsonPropertyName("activityType")]
    public string ActivityType { get; init; } = string.Empty;

    [JsonPropertyName("targetValue")]
    public string TargetValue { get; init; } = string.Empty;

    [JsonPropertyName("audio")]
    public AiAudioMetadataDto? Audio { get; init; }

    [JsonPropertyName("vad")]
    public AiVadResultDto? Vad { get; init; }

    [JsonPropertyName("transcription")]
    public AiTranscriptionResultDto? Transcription { get; init; }

    [JsonPropertyName("normalization")]
    public AiNormalizationResultDto? Normalization { get; init; }

    [JsonPropertyName("scores")]
    public AiScoreBreakdownDto? Scores { get; init; }
}

public sealed class AiAudioMetadataDto
{
    [JsonPropertyName("contentType")]
    public string? ContentType { get; init; }

    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; init; }

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("sampleRate")]
    public int? SampleRate { get; init; }

    [JsonPropertyName("channels")]
    public int? Channels { get; init; }
}

public sealed class AiVadResultDto
{
    [JsonPropertyName("speechDetected")]
    public bool SpeechDetected { get; init; }

    [JsonPropertyName("speechDurationSeconds")]
    public double SpeechDurationSeconds { get; init; }

    [JsonPropertyName("totalDurationSeconds")]
    public double TotalDurationSeconds { get; init; }

    [JsonPropertyName("speechRatio")]
    public double SpeechRatio { get; init; }

    [JsonPropertyName("segmentCount")]
    public int SegmentCount { get; init; }
}

public sealed class AiTranscriptionResultDto
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("languageProbability")]
    public double? LanguageProbability { get; init; }

    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; init; }

    [JsonPropertyName("averageConfidence")]
    public double? AverageConfidence { get; init; }

    [JsonPropertyName("model")]
    public string? Model { get; init; }
}

public sealed class AiNormalizationResultDto
{
    [JsonPropertyName("originalText")]
    public string? OriginalText { get; init; }

    [JsonPropertyName("normalizedText")]
    public string? NormalizedText { get; init; }

    [JsonPropertyName("normalizedTarget")]
    public string? NormalizedTarget { get; init; }

    [JsonPropertyName("normalizationRulesVersion")]
    public string? NormalizationRulesVersion { get; init; }
}

public sealed class AiScoreBreakdownDto
{
    [JsonPropertyName("accuracyScore")]
    public double AccuracyScore { get; init; }

    [JsonPropertyName("completenessScore")]
    public double CompletenessScore { get; init; }

    [JsonPropertyName("fluencyScore")]
    public double FluencyScore { get; init; }

    [JsonPropertyName("pronunciationProxyScore")]
    public double PronunciationProxyScore { get; init; }

    [JsonPropertyName("overallScore")]
    public double OverallScore { get; init; }

    [JsonPropertyName("matched")]
    public bool Matched { get; init; }

    [JsonPropertyName("reasonCodes")]
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();

    [JsonPropertyName("scoringMethod")]
    public string? ScoringMethod { get; init; }

    [JsonPropertyName("rulesVersion")]
    public string? RulesVersion { get; init; }
}
