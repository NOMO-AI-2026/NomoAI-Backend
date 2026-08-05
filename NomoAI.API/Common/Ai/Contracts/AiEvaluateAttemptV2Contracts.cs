using System.Text.Json.Serialization;

namespace NomoAI.API.Common.Ai.Contracts;

/// <summary>
/// V2 multipart attempt-evaluation payload. No sessionId/childId/activityId;
/// audio stream ownership remains with the caller — the client disposes multipart
/// content after the request completes.
/// </summary>
public sealed class AiEvaluateAttemptV2Request
{
    public required Stream AudioStream { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    /// <summary>character | word | sentence | conversation</summary>
    public required string ActivityType { get; init; }

    /// <summary>Expected target or conversation question/topic.</summary>
    public required string Prompt { get; init; }

    public required string SpeechLevel { get; init; }

    public required int Age { get; init; }

    public required int AttemptNumber { get; init; }

    public int? MaximumAttempts { get; init; }

    public IReadOnlyList<double>? PreviousAttemptScores { get; init; }

    public string? PreviousDecision { get; init; }

    public int ConsecutiveNoSpeechCount { get; init; }

    public string Language { get; init; } = "ar";
}

public sealed class AiEvaluateAttemptV2Response
{
    [JsonPropertyName("attemptNumber")]
    public int AttemptNumber { get; init; }

    [JsonPropertyName("activityType")]
    public string ActivityType { get; init; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } = string.Empty;

    [JsonPropertyName("speechOutcome")]
    public string SpeechOutcome { get; init; } = string.Empty;

    [JsonPropertyName("speechAnalysis")]
    public AiSpeechAnalysisV2Dto? SpeechAnalysis { get; init; }

    /// <summary>Discriminated by <see cref="AiEvaluateScoreDto.Kind"/> (target_based | conversation).</summary>
    [JsonPropertyName("scores")]
    public AiEvaluateScoreDto? Scores { get; init; }

    [JsonPropertyName("adaptiveDecision")]
    public required AiAdaptiveDecisionDto AdaptiveDecision { get; init; }

    [JsonPropertyName("knowledgeSourceIds")]
    public IReadOnlyList<Guid> KnowledgeSourceIds { get; init; } = Array.Empty<Guid>();

    [JsonPropertyName("knowledgeChunkIds")]
    public IReadOnlyList<Guid> KnowledgeChunkIds { get; init; } = Array.Empty<Guid>();

    [JsonPropertyName("avatar")]
    public required AiAvatarInstructionDto Avatar { get; init; }

    [JsonPropertyName("avatarModel")]
    public string? AvatarModel { get; init; }

    [JsonPropertyName("avatarGenerationMode")]
    public string AvatarGenerationMode { get; init; } = string.Empty;

    [JsonPropertyName("usedFallback")]
    public bool UsedFallback { get; init; }

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset? GeneratedAt { get; init; }
}

/// <summary>
/// Speech analysis without database identity fields. <see cref="Scores"/> merges the
/// target-based and conversation score-breakdown shapes; unused fields are simply absent.
/// </summary>
public sealed class AiSpeechAnalysisV2Dto
{
    [JsonPropertyName("activityType")]
    public string ActivityType { get; init; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } = string.Empty;

    [JsonPropertyName("audio")]
    public AiAudioMetadataDto? Audio { get; init; }

    [JsonPropertyName("vad")]
    public AiVadResultDto? Vad { get; init; }

    [JsonPropertyName("transcription")]
    public AiTranscriptionResultDto? Transcription { get; init; }

    [JsonPropertyName("normalization")]
    public AiNormalizationResultDto? Normalization { get; init; }

    [JsonPropertyName("scores")]
    public AiSpeechAnalysisScoreDto? Scores { get; init; }
}

/// <summary>
/// Merged shape covering FastAPI's ScoreBreakdown (target-based) and
/// ConversationScoreBreakdown, which are not discriminated in speechAnalysis.scores.
/// Only the fields relevant to the actual activity type will be populated.
/// </summary>
public sealed class AiSpeechAnalysisScoreDto
{
    [JsonPropertyName("accuracyScore")]
    public double? AccuracyScore { get; init; }

    [JsonPropertyName("completenessScore")]
    public double CompletenessScore { get; init; }

    [JsonPropertyName("fluencyScore")]
    public double FluencyScore { get; init; }

    [JsonPropertyName("pronunciationProxyScore")]
    public double? PronunciationProxyScore { get; init; }

    [JsonPropertyName("relevanceScore")]
    public double? RelevanceScore { get; init; }

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

    [JsonPropertyName("transcriptWordCount")]
    public int? TranscriptWordCount { get; init; }

    [JsonPropertyName("speechDurationSeconds")]
    public double? SpeechDurationSeconds { get; init; }
}

/// <summary>
/// Top-level V2 evaluate score view. FastAPI discriminates by "kind":
/// target_based (accuracy/pronunciation/fluency/completeness/overall/matched) or
/// conversation (relevance/fluency/completeness/overall/matched). Both shapes bind
/// into this one flexible DTO; irrelevant fields for the given kind stay null/default.
/// </summary>
public sealed class AiEvaluateScoreDto
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("accuracy")]
    public double? Accuracy { get; init; }

    [JsonPropertyName("pronunciation")]
    public double? Pronunciation { get; init; }

    [JsonPropertyName("relevance")]
    public double? Relevance { get; init; }

    [JsonPropertyName("fluency")]
    public double Fluency { get; init; }

    [JsonPropertyName("completeness")]
    public double Completeness { get; init; }

    [JsonPropertyName("overall")]
    public double Overall { get; init; }

    [JsonPropertyName("matched")]
    public bool Matched { get; init; }

    [JsonPropertyName("scoringMethod")]
    public string ScoringMethod { get; init; } = string.Empty;

    [JsonPropertyName("reasonCodes")]
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();

    [JsonPropertyName("transcriptWordCount")]
    public int? TranscriptWordCount { get; init; }

    [JsonPropertyName("speechDurationSeconds")]
    public double? SpeechDurationSeconds { get; init; }

    public bool IsTargetBased => string.Equals(Kind, "target_based", StringComparison.OrdinalIgnoreCase);
}
