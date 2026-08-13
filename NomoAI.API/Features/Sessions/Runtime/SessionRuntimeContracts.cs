using System.Text.Json;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Infrastructure.Ai;

namespace NomoAI.API.Features.Sessions.Runtime;

/// <summary>Command values telling the React client what to do next.</summary>
public static class SessionRuntimeCommand
{
    public const string PlayAvatarSpeech = "play_avatar_speech";
    public const string ReadyToRecord = "ready_to_record";
    public const string TakeBreak = "take_break";
    public const string SessionCompleted = "session_completed";
    public const string PlayFeedback = "play_feedback";
}

/// <summary>Persisted session lifecycle status exposed over HTTP as camelCase strings.</summary>
public static class SessionRuntimeStatus
{
    public const string Scheduled = "scheduled";
    public const string InProgress = "inProgress";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string Missed = "missed";

    public static string Map(SessionStatus status) => status switch
    {
        SessionStatus.Scheduled => Scheduled,
        SessionStatus.InProgress => InProgress,
        SessionStatus.Completed => Completed,
        SessionStatus.Cancelled => Cancelled,
        SessionStatus.Missed => Missed,
        _ => InProgress
    };
}

/// <summary>Adaptive-decision action values returned by AI Core's adaptiveDecision.action.</summary>
internal static class AdaptiveActions
{
    public const string Advance = "advance";
    public const string RetrySame = "retry_same";
    public const string RetryWithHint = "retry_with_hint";
    public const string Simplify = "simplify";
    public const string AskFollowUp = "ask_follow_up";
    public const string ContinueConversation = "continue_conversation";
    public const string End = "end";
    public const string EndAttempts = "end_attempts";
    public const string RecommendDoctorReview = "recommend_doctor_review";
    public const string TakeShortBreak = "take_short_break";
    public const string Model = "model";
    public const string SlowDown = "slow_down";

    public static readonly IReadOnlySet<string> Known = new HashSet<string>(StringComparer.Ordinal)
    {
        Advance,
        RetrySame,
        RetryWithHint,
        Simplify,
        Model,
        SlowDown,
        AskFollowUp,
        ContinueConversation,
        End,
        EndAttempts,
        RecommendDoctorReview,
        TakeShortBreak
    };

    public static bool IsKnown(string? action) =>
        !string.IsNullOrWhiteSpace(action) && Known.Contains(action);
}

public sealed class SessionRuntimeResponse
{
    public int SessionId { get; init; }

    public string Status { get; init; } = SessionRuntimeStatus.InProgress;

    public SessionRuntimeStepDto? CurrentStep { get; init; }

    public string Command { get; init; } = SessionRuntimeCommand.PlayAvatarSpeech;

    public SessionFeedbackDto? Feedback { get; init; }

    public string? ActivityType { get; init; }

    public string? Prompt { get; init; }

    /// <summary>
    /// Optional pre-synthesized avatar/instruction audio for <c>play_avatar_speech</c>.
    /// When present the React client skips GET .../speech.
    /// </summary>
    public string? SpeechAudioBase64 { get; init; }

    public string? SpeechAudioContentType { get; init; }
}

public sealed class SessionRuntimeStepDto
{
    public int StepNumber { get; init; }

    public string StepType { get; init; } = string.Empty;

    public string SpokenText { get; init; } = string.Empty;

    public bool ExpectsChildResponse { get; init; }

    public int MaximumAttempts { get; init; }

    public int AttemptNumber { get; init; }

    public string? AvatarEmotion { get; init; }
}

public sealed class SessionFeedbackDto
{
    public int AttemptId { get; init; }

    public string AdaptiveAction { get; init; } = string.Empty;

    public string SpokenText { get; init; } = string.Empty;

    public string? Emotion { get; init; }

    public SessionFeedbackScoreDto? Scores { get; init; }

    /// <summary>
    /// Optional pre-synthesized feedback audio (base64). When present the React
    /// client plays it immediately and skips GET .../feedback-speech.
    /// </summary>
    public string? AudioBase64 { get; init; }

    public string? AudioContentType { get; init; }
}

public sealed class SessionFeedbackScoreDto
{
    public double? Accuracy { get; init; }

    public double? Pronunciation { get; init; }

    public double? Fluency { get; init; }

    public double? Completeness { get; init; }

    public double? Relevance { get; init; }

    public double Overall { get; init; }

    public bool Matched { get; init; }
}

/// <summary>
/// Session step types that expect a spoken response from the child. Mirrors
/// app.schemas.session_plan.SessionStepType on the FastAPI side.
/// </summary>
internal static class SessionStepTypes
{
    private static readonly string[] SpeakingKeywords =
    [
        "repeat", "say", "practice", "speak",
        "كرر", "قول", "قل", "انطق", "تكلم"
    ];

    public static bool ExpectsChildResponse(string stepType, string expectedChildAction)
    {
        string normalizedType = (stepType ?? string.Empty).Trim().ToLowerInvariant();

        if (normalizedType is "guided_practice" or "independent_attempt")
        {
            return true;
        }

        if (normalizedType is "introduction" or "demonstration" or "completion" or "review")
        {
            return false;
        }

        // "reinforcement" (and any future step type) — infer from the instruction text.
        string action = expectedChildAction ?? string.Empty;
        return SpeakingKeywords.Any(keyword => action.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Child-response steps get a generous retry floor so short AI plans
    /// (often maximumAttempts=2) do not end the therapy turn too early.
    /// </summary>
    public const int MinimumSpeakingAttempts = 5;

    public static int EffectiveMaximumAttempts(string stepType, string expectedChildAction, int plannedMaximum)
    {
        int capped = Math.Max(1, plannedMaximum);
        if (ExpectsChildResponse(stepType, expectedChildAction))
        {
            return Math.Max(MinimumSpeakingAttempts, capped);
        }

        return capped;
    }
}

/// <summary>
/// Serializes/deserializes the AiSessionPlanV2Response snapshot persisted on
/// Session.PlanJson, reusing AI Core's camelCase + string-enum JSON options.
/// </summary>
internal static class SessionPlanSnapshot
{
    public static string Serialize(AiSessionPlanV2Response plan) =>
        JsonSerializer.Serialize(plan, AiCoreJsonSerializerOptions.Instance);

    public static AiSessionPlanV2Response? Deserialize(string? planJson)
    {
        if (string.IsNullOrWhiteSpace(planJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiSessionPlanV2Response>(
                planJson,
                AiCoreJsonSerializerOptions.Instance);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static AiSessionStepDto? GetStep(AiSessionPlanV2Response plan, int stepNumber) =>
        plan.Steps.FirstOrDefault(step => step.StepNumber == stepNumber);
}
