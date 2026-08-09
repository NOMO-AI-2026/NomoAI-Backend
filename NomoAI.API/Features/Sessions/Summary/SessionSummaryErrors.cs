using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Sessions.Summary;

public static class SessionSummaryErrors
{
    public static readonly Error SessionNotFound = new(
        "SessionSummary.SessionNotFound",
        "Session not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error ChildNotFound = new(
        "SessionSummary.ChildNotFound",
        "Child not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error Forbidden = new(
        "SessionSummary.Forbidden",
        "You do not have permission to access this session summary.",
        StatusCodes.Status403Forbidden);

    public static readonly Error SessionNotCompleted = new(
        "SessionSummary.SessionNotCompleted",
        "Summary can only be generated for a completed session.",
        StatusCodes.Status409Conflict);

    public static readonly Error NoAttempts = new(
        "SessionSummary.NoAttempts",
        "This session has no recorded attempts to summarize.",
        StatusCodes.Status409Conflict);

    public static readonly Error SummaryNotFound = new(
        "SessionSummary.SummaryNotFound",
        "Session summary was not found. Generate it first.",
        StatusCodes.Status404NotFound);

    public static readonly Error GenerationFailed = new(
        "SessionSummary.GenerationFailed",
        "Session summary generation failed.",
        StatusCodes.Status502BadGateway);
}
