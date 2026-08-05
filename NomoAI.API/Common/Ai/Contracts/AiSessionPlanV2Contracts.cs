using System.Text.Json.Serialization;

namespace NomoAI.API.Common.Ai.Contracts;

/// <summary>
/// V2 plan request body — therapy content only. No sessionId/childId/activityId;
/// database identity stays on the ASP.NET side and is never sent to AI Core.
/// </summary>
public sealed class AiSessionPlanV2Request
{
    /// <summary>character | word | sentence | conversation</summary>
    [JsonPropertyName("activityType")]
    public required string ActivityType { get; init; }

    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    [JsonPropertyName("speechLevel")]
    public required string SpeechLevel { get; init; }

    [JsonPropertyName("age")]
    public required int Age { get; init; }

    [JsonPropertyName("language")]
    public string Language { get; init; } = "ar";

    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; init; } = 15;

    [JsonPropertyName("maxSteps")]
    public int MaxSteps { get; init; } = 6;

    [JsonPropertyName("previousSummary")]
    public string? PreviousSummary { get; init; }

    [JsonPropertyName("doctorContext")]
    public string? DoctorContext { get; init; }
}

public sealed class AiSessionPlanV2Response
{
    [JsonPropertyName("activityType")]
    public string ActivityType { get; init; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } = string.Empty;

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
