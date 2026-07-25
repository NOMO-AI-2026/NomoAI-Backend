using System.Text.Json.Serialization;

namespace NomoAI.API.Common.Ai.Contracts;

public sealed class AiSessionPlanRequest
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; init; }

    [JsonPropertyName("childId")]
    public required string ChildId { get; init; }

    [JsonPropertyName("activityId")]
    public required string ActivityId { get; init; }

    /// <summary>character | word | sentence</summary>
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

    [JsonPropertyName("maximumDurationMinutes")]
    public int MaximumDurationMinutes { get; init; } = 15;

    [JsonPropertyName("maximumSteps")]
    public int MaximumSteps { get; init; } = 6;

    [JsonPropertyName("previousSessionSummary")]
    public string? PreviousSessionSummary { get; init; }

    [JsonPropertyName("additionalContext")]
    public string? AdditionalContext { get; init; }
}

public sealed class AiSessionPlanResponse
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; init; }

    [JsonPropertyName("activityId")]
    public string ActivityId { get; init; } = string.Empty;

    [JsonPropertyName("activityType")]
    public string ActivityType { get; init; } = string.Empty;

    [JsonPropertyName("targetValue")]
    public string TargetValue { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("objective")]
    public string Objective { get; init; } = string.Empty;

    [JsonPropertyName("estimatedDurationMinutes")]
    public int EstimatedDurationMinutes { get; init; }

    [JsonPropertyName("steps")]
    public IReadOnlyList<AiSessionStepDto> Steps { get; init; } = Array.Empty<AiSessionStepDto>();

    [JsonPropertyName("completionCriteria")]
    public IReadOnlyList<string> CompletionCriteria { get; init; } = Array.Empty<string>();

    [JsonPropertyName("safetyNotes")]
    public IReadOnlyList<string> SafetyNotes { get; init; } = Array.Empty<string>();

    [JsonPropertyName("knowledgeSourceIds")]
    public IReadOnlyList<Guid> KnowledgeSourceIds { get; init; } = Array.Empty<Guid>();

    [JsonPropertyName("knowledgeChunkIds")]
    public IReadOnlyList<Guid> KnowledgeChunkIds { get; init; } = Array.Empty<Guid>();

    [JsonPropertyName("model")]
    public string? Model { get; init; }

    [JsonPropertyName("usedFallback")]
    public bool UsedFallback { get; init; }

    [JsonPropertyName("regenerated")]
    public bool Regenerated { get; init; }

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset? GeneratedAt { get; init; }
}

public sealed class AiSessionStepDto
{
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("instruction")]
    public string Instruction { get; init; } = string.Empty;

    [JsonPropertyName("expectedChildAction")]
    public string ExpectedChildAction { get; init; } = string.Empty;

    [JsonPropertyName("maximumAttempts")]
    public int MaximumAttempts { get; init; }

    [JsonPropertyName("successCriteria")]
    public string SuccessCriteria { get; init; } = string.Empty;

    [JsonPropertyName("avatar")]
    public AiAvatarInstructionDto? Avatar { get; init; }
}
