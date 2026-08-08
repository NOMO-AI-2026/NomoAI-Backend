namespace NomoAI.API.Features.Sessions.Runtime.GetSessionAttempts;

public sealed class GetSessionAttemptsResponse
{
    public int SessionId { get; init; }

    public int ChildId { get; init; }

    public int TotalAttempts { get; init; }

    public IReadOnlyList<SessionAttemptItemResponse> Attempts { get; init; } =
        Array.Empty<SessionAttemptItemResponse>();
}

public sealed class SessionAttemptItemResponse
{
    public int AttemptId { get; init; }

    public int AttemptNumber { get; init; }

    public string? AudioUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public SessionAttemptEvaluationResponse? Evaluation { get; init; }

    public SessionAttemptTranscriptionResponse? Transcription { get; init; }
}

public sealed class SessionAttemptEvaluationResponse
{
    public decimal AccuracyScore { get; init; }

    public decimal FluencyScore { get; init; }

    public decimal PronunciationScore { get; init; }

    public decimal CompletenessScore { get; init; }

    public decimal OverallScore { get; init; }

    public bool IsSuccessful { get; init; }

    public string? SpeechOutcome { get; init; }

    public string? AdaptiveAction { get; init; }

    public bool? Matched { get; init; }

    public string? Feedback { get; init; }

    public string? AvatarSpokenText { get; init; }

    public string? AvatarEmotion { get; init; }

    public string? NormalizedTranscript { get; init; }
}

public sealed class SessionAttemptTranscriptionResponse
{
    public string TranscribedText { get; init; } = string.Empty;

    public string? NormalizedText { get; init; }

    public string DetectedLanguage { get; init; } = string.Empty;
}
