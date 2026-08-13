using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Ai.Contracts;
using NomoAI.API.Common.Options;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Sessions.Runtime.GetSessionRuntime;
using NomoAI.API.Features.Sessions.Runtime.StartSession;
using NomoAI.API.Infrastructure.Ai;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace NomoAI.API.Features.Sessions.Runtime.SubmitAttempt;

/// <summary>
/// MediatR command uses Stream metadata instead of IFormFile to keep the handler
/// transport-agnostic. The endpoint owns opening/disposing the audio stream around Send.
/// </summary>
public sealed record SubmitAttemptCommand(
    int SessionId,
    Stream? AudioStream,
    string FileName,
    string ContentType,
    long AudioLengthBytes,
    string UserId) : IRequest<Result<SessionRuntimeResponse>>;

public sealed class SubmitAttemptCommandValidator : AbstractValidator<SubmitAttemptCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/wav",
        "audio/x-wav",
        "audio/mpeg",
        "audio/mp3",
        "audio/mp4",
        "audio/x-m4a",
        "audio/m4a",
        "audio/webm",
        "audio/ogg",
        "application/ogg",
        "application/octet-stream"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".mp3", ".m4a", ".webm", ".ogg"
    };

    public SubmitAttemptCommandValidator(IOptions<AiServiceOptions> options)
    {
        long maxBytes = options.Value.MaxAudioBytes;

        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.AudioStream)
            .NotNull()
            .WithMessage("Audio file is required.");

        RuleFor(x => x.AudioLengthBytes)
            .GreaterThan(0)
            .WithMessage("Audio file must not be empty.")
            .LessThanOrEqualTo(maxBytes)
            .WithMessage($"Audio file must not exceed {maxBytes} bytes.");

        RuleFor(x => x.ContentType)
            .Must(BeAllowedContentType)
            .WithMessage("Audio content type is not supported.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(HaveAllowedExtension)
            .WithMessage("Audio file extension is not supported.");
    }

    private static bool BeAllowedContentType(string? contentType)
    {
        string normalized = (contentType ?? string.Empty).Split(';')[0].Trim();
        return string.IsNullOrWhiteSpace(normalized) || AllowedContentTypes.Contains(normalized);
    }

    private static bool HaveAllowedExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return AllowedExtensions.Contains(Path.GetExtension(fileName));
    }
}

internal sealed class SubmitAttemptCommandHandler
    : IRequestHandler<SubmitAttemptCommand, Result<SessionRuntimeResponse>>
{
    private const string NoSpeechOutcome = "no_speech";

    private readonly AppDbContext _db;
    private readonly IAiCoreClient _aiCoreClient;
    private readonly PublicApiOptions _publicApiOptions;

    public SubmitAttemptCommandHandler(
        AppDbContext db,
        IAiCoreClient aiCoreClient,
        IOptions<PublicApiOptions> publicApiOptions)
    {
        _db = db;
        _aiCoreClient = aiCoreClient;
        _publicApiOptions = publicApiOptions.Value;
    }

    public async Task<Result<SessionRuntimeResponse>> Handle(
        SubmitAttemptCommand request,
        CancellationToken cancellationToken)
    {
        (ChildOwnershipStatus ownership, Session? session) = await SessionOwnership.CheckSessionOwnershipAsync(
            _db, request.UserId, request.SessionId, cancellationToken);

        if (ownership == ChildOwnershipStatus.ChildNotFound || session is null)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.SessionNotFound);
        }

        if (ownership == ChildOwnershipStatus.Forbidden)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.Forbidden);
        }

        AiSessionPlanV2Response? plan = SessionPlanSnapshot.Deserialize(session.PlanJson);
        if (SessionLifecycle.SynchronizePersistedStatus(session, plan) &&
            session.Status == SessionStatus.Completed)
        {
            await SessionCompletion.FinalizeAsync(_db, session, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success(GetSessionRuntimeQueryHandler.BuildRuntimeResponse(session));
        }

        if (!SessionLifecycle.IsRunnable(session))
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.SessionNotInProgress);
        }
        if (plan is null)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.PlanUnavailable);
        }

        AiSessionStepDto? step = SessionPlanSnapshot.GetStep(plan, session.CurrentStepNumber);
        if (step is null)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.StepNotFound);
        }

        if (session.CurrentAttemptNumber >= SessionStepTypes.EffectiveMaximumAttempts(
                step.Type, step.ExpectedChildAction, step.MaximumAttempts))
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.MaximumAttemptsExceeded);
        }

        int globalAttemptNumber = await _db.SessionAttempts
            .CountAsync(a => a.SessionId == session.Id && !a.IsDeleted, cancellationToken) + 1;

        bool alreadyRecorded = await _db.SessionAttempts
            .AnyAsync(
                a => a.SessionId == session.Id && a.AttemptNumber == globalAttemptNumber && !a.IsDeleted,
                cancellationToken);

        if (alreadyRecorded)
        {
            return Result.Failure<SessionRuntimeResponse>(SessionRuntimeErrors.DuplicateAttempt);
        }

        // Buffer before evaluate so the same child bytes can be sent to AI and persisted.
        Result<byte[]> audioBytesResult = await ReadChildAudioBytesAsync(request, cancellationToken);
        if (audioBytesResult.IsFailure)
        {
            return Result.Failure<SessionRuntimeResponse>(audioBytesResult.Error);
        }

        byte[] childAudioBytes = audioBytesResult.Value;
        string audioContentType = string.IsNullOrWhiteSpace(request.ContentType)
            ? "application/octet-stream"
            : request.ContentType.Split(';')[0].Trim();

        (IReadOnlyList<double> previousScores,
            string? previousDecision,
            int consecutiveNoSpeechCount,
            AiSessionExecutionDto? previousExecution) =
            await LoadAttemptHistoryAsync(session, cancellationToken);

        int stepAttemptNumber = session.CurrentAttemptNumber + 1;
        int age = session.ChildAge ?? 6;
        int effectiveMaxAttempts = SessionStepTypes.EffectiveMaximumAttempts(
            step.Type, step.ExpectedChildAction, step.MaximumAttempts);

        // Always send the doctor/session original prompt. practiceTarget lives only in sessionExecution.
        await using var evaluateStream = new MemoryStream(childAudioBytes, writable: false);
        var aiRequest = new AiEvaluateAttemptV2Request
        {
            AudioStream = evaluateStream,
            FileName = Path.GetFileName(request.FileName),
            ContentType = audioContentType,
            ActivityType = session.ActivityType ?? plan.ActivityType,
            Prompt = session.Prompt ?? plan.Prompt,
            SpeechLevel = session.SpeechLevel ?? string.Empty,
            Age = age,
            AttemptNumber = stepAttemptNumber,
            MaximumAttempts = effectiveMaxAttempts,
            PreviousAttemptScores = previousScores.Count == 0 ? null : previousScores,
            PreviousDecision = previousDecision,
            ConsecutiveNoSpeechCount = consecutiveNoSpeechCount,
            Language = session.Language,
            SessionExecution = previousExecution
        };

        Result<AiEvaluateAttemptV2Response> evaluateResult =
            await _aiCoreClient.EvaluateAttemptAsync(aiRequest, cancellationToken);

        if (evaluateResult.IsFailure)
        {
            return Result.Failure<SessionRuntimeResponse>(evaluateResult.Error);
        }

        AiEvaluateAttemptV2Response evaluation = evaluateResult.Value;
        string spokenText = evaluation.Avatar.SpokenText ?? string.Empty;

        // Start TTS immediately while we persist — cuts a full client round-trip later.
        Task<(string? Base64, string? ContentType)> feedbackAudioTask =
            SessionSpeechEmbedder.TryBufferAsync(
                _aiCoreClient,
                spokenText,
                purpose: "encouragement",
                cancellationToken);

        var attempt = new SessionAttempts
        {
            SessionId = session.Id,
            AttemptNumber = globalAttemptNumber,
            AudioContent = childAudioBytes,
            AudioContentType = audioContentType,
            AudioUrl = null
        };

        _db.SessionAttempts.Add(attempt);

        AiEvaluateScoreDto? scores = evaluation.Scores;
        AiSpeechAnalysisScoreDto? analysisScores = evaluation.SpeechAnalysis?.Scores;

        _db.AttemptTranscribtions.Add(new AttemptTranscribtion
        {
            Attempt = attempt,
            TranscribedText = evaluation.SpeechAnalysis?.Transcription?.Text ?? string.Empty,
            DetectedLanguage = evaluation.SpeechAnalysis?.Transcription?.Language ?? session.Language,
            NormalizedText = evaluation.SpeechAnalysis?.Normalization?.NormalizedText
        });

        _db.AttemptEvaluations.Add(new AttemptEvaluation
        {
            Attempt = attempt,
            AccuracyScore = (decimal)(scores?.Accuracy ?? analysisScores?.AccuracyScore ?? 0),
            FluencyScore = (decimal)(scores?.Fluency ?? analysisScores?.FluencyScore ?? 0),
            PronunciationScore = (decimal)(scores?.Pronunciation ?? analysisScores?.PronunciationProxyScore ?? 0),
            CompletenessScore = (decimal)(scores?.Completeness ?? analysisScores?.CompletenessScore ?? 0),
            Feedback = spokenText,
            IsSuccessful = scores?.Matched ?? analysisScores?.Matched ?? false,
            AdaptiveAction = evaluation.AdaptiveDecision.Action,
            AvatarSpokenText = spokenText,
            AvatarEmotion = evaluation.Avatar.Emotion,
            SpeechOutcome = evaluation.SpeechOutcome,
            Matched = scores?.Matched ?? analysisScores?.Matched,
            NormalizedTranscript = evaluation.SpeechAnalysis?.Normalization?.NormalizedText,
            EvaluationJson = JsonSerializer.Serialize(evaluation, AiCoreJsonSerializerOptions.Instance),
            KnowledgeSourceIdsJson = JsonSerializer.Serialize(evaluation.KnowledgeSourceIds),
            KnowledgeChunkIdsJson = JsonSerializer.Serialize(evaluation.KnowledgeChunkIds)
        });

        ApplyAdaptiveTransition(session, plan, evaluation.AdaptiveDecision.Action);

        if (session.Status == SessionStatus.Completed)
        {
            await SessionCompletion.FinalizeAsync(_db, session, cancellationToken);
        }

        // Persist attempt + audio bytes first so we never store an AudioUrl without content.
        await _db.SaveChangesAsync(cancellationToken);

        attempt.AudioUrl = AttemptAudioUrl.Build(session.Id, attempt.Id, _publicApiOptions.BaseUrl);
        await _db.SaveChangesAsync(cancellationToken);

        (string? audioBase64, string? audioContentTypeResponse) = await feedbackAudioTask;

        var feedback = new SessionFeedbackDto
        {
            AttemptId = attempt.Id,
            AdaptiveAction = evaluation.AdaptiveDecision.Action,
            SpokenText = spokenText,
            Emotion = evaluation.Avatar.Emotion,
            Scores = new SessionFeedbackScoreDto
            {
                Accuracy = scores?.Accuracy,
                Pronunciation = scores?.Pronunciation,
                Fluency = scores?.Fluency ?? analysisScores?.FluencyScore,
                Completeness = scores?.Completeness ?? analysisScores?.CompletenessScore,
                Relevance = scores?.Relevance,
                Overall = scores?.Overall ?? analysisScores?.OverallScore ?? 0,
                Matched = scores?.Matched ?? analysisScores?.Matched ?? false
            },
            AudioBase64 = audioBase64,
            AudioContentType = audioContentTypeResponse
        };

        SessionRuntimeResponse response = BuildResponse(session, plan, evaluation.AdaptiveDecision.Action, feedback);
        return Result.Success(response);
    }

    private static async Task<Result<byte[]>> ReadChildAudioBytesAsync(
        SubmitAttemptCommand request,
        CancellationToken cancellationToken)
    {
        if (request.AudioStream is null)
        {
            return Result.Failure<byte[]>(SessionRuntimeErrors.AudioRequired);
        }

        try
        {
            await using var buffer = new MemoryStream(
                capacity: request.AudioLengthBytes > 0 && request.AudioLengthBytes <= int.MaxValue
                    ? (int)request.AudioLengthBytes
                    : 0);

            await request.AudioStream.CopyToAsync(buffer, cancellationToken);
            byte[] bytes = buffer.ToArray();

            if (bytes.Length == 0)
            {
                return Result.Failure<byte[]>(SessionRuntimeErrors.AudioRequired);
            }

            return Result.Success(bytes);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<byte[]>(SessionRuntimeErrors.AttemptAudioPersistFailed);
        }
    }

    private async Task<(
        IReadOnlyList<double> PreviousScores,
        string? PreviousDecision,
        int ConsecutiveNoSpeechCount,
        AiSessionExecutionDto? SessionExecution)>
        LoadAttemptHistoryAsync(Session session, CancellationToken cancellationToken)
    {
        if (session.CurrentAttemptNumber <= 0)
        {
            return (Array.Empty<double>(), null, 0, null);
        }

        // Attempts made on the current step are exactly the last CurrentAttemptNumber
        // rows recorded for this session (CurrentAttemptNumber resets to 0 on advance).
        List<(int AttemptNumber, string? AdaptiveAction, string? SpeechOutcome, string? EvaluationJson)> recent =
            await _db.SessionAttempts
                .Where(a => a.SessionId == session.Id && !a.IsDeleted)
                .OrderByDescending(a => a.AttemptNumber)
                .Take(session.CurrentAttemptNumber)
                .Join(
                    _db.AttemptEvaluations.Where(e => !e.IsDeleted),
                    a => a.Id,
                    e => e.AttemptId,
                    (a, e) => new { a.AttemptNumber, e.AdaptiveAction, e.SpeechOutcome, e.EvaluationJson })
                .Select(x => new ValueTuple<int, string?, string?, string?>(
                    x.AttemptNumber, x.AdaptiveAction, x.SpeechOutcome, x.EvaluationJson))
                .ToListAsync(cancellationToken);

        // recent is newest-first; reverse for chronological (oldest-first) score history.
        recent.Reverse();

        var scores = new List<double>(recent.Count);
        foreach (var row in recent)
        {
            AiEvaluateAttemptV2Response? snapshot =
                SessionExecutionSnapshot.TryDeserializeEvaluation(row.EvaluationJson);

            double? overall = snapshot?.Scores?.Overall ?? snapshot?.SpeechAnalysis?.Scores?.OverallScore;
            if (overall is not null)
            {
                scores.Add(overall.Value);
            }
        }

        string? previousDecision = recent.Count == 0 ? null : recent[^1].AdaptiveAction;
        AiSessionExecutionDto? previousExecution = recent.Count == 0
            ? null
            : SessionExecutionSnapshot.TryReadFromEvaluationJson(recent[^1].EvaluationJson);

        int consecutiveNoSpeechCount = 0;
        for (int i = recent.Count - 1; i >= 0; i--)
        {
            if (string.Equals(recent[i].SpeechOutcome, NoSpeechOutcome, StringComparison.OrdinalIgnoreCase))
            {
                consecutiveNoSpeechCount++;
            }
            else
            {
                break;
            }
        }

        return (scores, previousDecision, consecutiveNoSpeechCount, previousExecution);
    }

    internal static void ApplyAdaptiveTransition(Session session, AiSessionPlanV2Response plan, string action)
    {
        switch (action)
        {
            case AdaptiveActions.Advance:
                AdvanceOrComplete(session, plan);
                break;

            case AdaptiveActions.End:
                // Explicit session end from the adaptive engine only.
                SessionLifecycle.MarkCompleted(session);
                break;

            case AdaptiveActions.EndAttempts:
                // Exhausted this step's practice budget — keep the session open and move on.
                AdvanceOrComplete(session, plan);
                break;

            case AdaptiveActions.RecommendDoctorReview:
                // Flag for clinicians but do not abruptly end the child's session.
                session.RequiresDoctorReview = true;
                AdvanceOrComplete(session, plan);
                break;

            case AdaptiveActions.RetrySame:
            case AdaptiveActions.RetryWithHint:
            case AdaptiveActions.Simplify:
            case AdaptiveActions.Model:
            case AdaptiveActions.SlowDown:
            case AdaptiveActions.AskFollowUp:
            case AdaptiveActions.ContinueConversation:
            case AdaptiveActions.TakeShortBreak:
            default:
            {
                session.CurrentAttemptNumber++;

                AiSessionStepDto? cappedStep = SessionPlanSnapshot.GetStep(plan, session.CurrentStepNumber);
                int maximumAttempts = cappedStep is null
                    ? SessionStepTypes.MinimumSpeakingAttempts
                    : SessionStepTypes.EffectiveMaximumAttempts(
                        cappedStep.Type, cappedStep.ExpectedChildAction, cappedStep.MaximumAttempts);

                if (session.CurrentAttemptNumber < maximumAttempts)
                {
                    break;
                }

                AdvanceOrComplete(session, plan);
                break;
            }
        }
    }

    /// <summary>Move to the next plan step, or complete only when no steps remain.</summary>
    private static void AdvanceOrComplete(Session session, AiSessionPlanV2Response plan)
    {
        session.CurrentAttemptNumber = 0;
        int nextStepNumber = session.CurrentStepNumber + 1;

        if (SessionPlanSnapshot.GetStep(plan, nextStepNumber) is null)
        {
            SessionLifecycle.MarkCompleted(session);
        }
        else
        {
            session.CurrentStepNumber = nextStepNumber;
        }
    }

    internal static SessionRuntimeResponse BuildResponse(
        Session session,
        AiSessionPlanV2Response plan,
        string adaptiveAction,
        SessionFeedbackDto feedback)
    {
        string status = SessionRuntimeStatus.Map(session.Status);
        AiSessionStepDto? currentStep = SessionPlanSnapshot.GetStep(plan, session.CurrentStepNumber);

        // Always return play_feedback when this submit produced avatar speech so the
        // client can play TTS even if the adaptive transition just completed the session.
        string command = adaptiveAction == AdaptiveActions.TakeShortBreak
                && session.Status == SessionStatus.InProgress
            ? SessionRuntimeCommand.TakeBreak
            : SessionRuntimeCommand.PlayFeedback;

        return new SessionRuntimeResponse
        {
            SessionId = session.Id,
            Status = status,
            CurrentStep = currentStep is null
                ? null
                : StartSessionCommandHandler.MapStep(currentStep, session.CurrentAttemptNumber),
            Command = command,
            Feedback = feedback,
            ActivityType = session.ActivityType,
            Prompt = session.Prompt
        };
    }
}

public static class SubmitAttemptEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/{sessionId:int}/attempts", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor", "Parent"))
            // Bearer JWT APIs do not use cookie antiforgery. Disable only on this
            // multipart Minimal API endpoint (not globally).
            .DisableAntiforgery()
            .WithName("SubmitSessionAttempt")
            .WithSummary("Submit a recorded attempt for evaluation and advance the session")
            .Produces<SessionRuntimeResponse>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound)
            .Produces<Error>(StatusCodes.Status409Conflict)
            .Produces<Error>(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleAsync(
        int sessionId,
        IFormFile? audio,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        string fileName = audio is null ? string.Empty : Path.GetFileName(audio.FileName);
        string contentType = audio is null
            ? string.Empty
            : (audio.ContentType ?? "application/octet-stream").Split(';')[0].Trim();
        long length = audio?.Length ?? 0;

        // Endpoint owns the stream lifetime for the duration of the MediatR call.
        await using Stream? audioStream = audio?.OpenReadStream();

        var command = new SubmitAttemptCommand(sessionId, audioStream, fileName, contentType, length, userId);

        Result<SessionRuntimeResponse> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(result.Error, statusCode: result.Error.StatusCode);
        }

        return Results.Ok(result.Value);
    }
}
