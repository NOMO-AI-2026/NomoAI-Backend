using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Sessions.Runtime;

public static class SessionRuntimeErrors
{
    public static readonly Error DoctorProfileNotFound = new(
        "SessionRuntime.DoctorProfileNotFound",
        "Doctor profile was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error ParentProfileNotFound = new(
        "SessionRuntime.ParentProfileNotFound",
        "Parent profile was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error DoctorNotApproved = new(
        "SessionRuntime.DoctorNotApproved",
        "The doctor account has not been approved.",
        StatusCodes.Status403Forbidden);

    public static readonly Error ChildNotFound = new(
        "SessionRuntime.ChildNotFound",
        "Child not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error ActivityNotFound = new(
        "SessionRuntime.ActivityNotFound",
        "Activity not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error ActivityDoesNotBelongToChild = new(
        "SessionRuntime.ActivityDoesNotBelongToChild",
        "The activity does not belong to this child.",
        StatusCodes.Status400BadRequest);

    public static readonly Error ActivitySessionAlreadyCreated = new(
        "SessionRuntime.ActivitySessionAlreadyCreated",
        "This activity was already used to complete a session and cannot start another.",
        StatusCodes.Status409Conflict);

    public static readonly Error SpeechLevelNotFound = new(
        "SessionRuntime.SpeechLevelNotFound",
        "The child's speech level was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error Forbidden = new(
        "SessionRuntime.Forbidden",
        "You do not have permission to access this session.",
        StatusCodes.Status403Forbidden);

    public static readonly Error SessionNotFound = new(
        "SessionRuntime.SessionNotFound",
        "Session not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error AttemptNotFound = new(
        "SessionRuntime.AttemptNotFound",
        "Attempt not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error FeedbackNotAvailable = new(
        "SessionRuntime.FeedbackNotAvailable",
        "No avatar feedback is available for this attempt.",
        StatusCodes.Status404NotFound);

    public static readonly Error SessionNotInProgress = new(
        "SessionRuntime.SessionNotInProgress",
        "This session is not in progress.",
        StatusCodes.Status409Conflict);

    public static readonly Error PlanUnavailable = new(
        "SessionRuntime.PlanUnavailable",
        "The session plan could not be loaded.",
        StatusCodes.Status500InternalServerError);

    public static readonly Error StepNotFound = new(
        "SessionRuntime.StepNotFound",
        "The current session step could not be found in the plan.",
        StatusCodes.Status500InternalServerError);

    public static readonly Error MaximumAttemptsExceeded = new(
        "SessionRuntime.MaximumAttemptsExceeded",
        "The maximum number of attempts for this step has already been reached.",
        StatusCodes.Status409Conflict);

    public static readonly Error DuplicateAttempt = new(
        "SessionRuntime.DuplicateAttempt",
        "This attempt has already been recorded.",
        StatusCodes.Status409Conflict);

    public static readonly Error AudioRequired = new(
        "SessionRuntime.AudioRequired",
        "Audio file is required.",
        StatusCodes.Status400BadRequest);

    public static readonly Error NoSpokenTextAvailable = new(
        "SessionRuntime.NoSpokenTextAvailable",
        "There is no spoken text available to synthesize for this step.",
        StatusCodes.Status404NotFound);

    public static readonly Error ChildResponseRequired = new(
        "SessionRuntime.ChildResponseRequired",
        "This step expects a child speaking attempt and cannot be continued without audio.",
        StatusCodes.Status409Conflict);
}
