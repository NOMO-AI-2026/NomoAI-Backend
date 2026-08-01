using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Support;

public static class SupportErrors
{
    public static readonly Error TicketNotFound = new(
        "Support.TicketNotFound",
        "Support ticket was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error Forbidden = new(
        "Support.Forbidden",
        "You do not have permission to access this support ticket.",
        StatusCodes.Status403Forbidden);

    public static readonly Error TicketLockedByAdmin = new(
        "Support.TicketLockedByAdmin",
        "This support ticket can no longer be edited or deleted because an admin has already taken action on it.",
        StatusCodes.Status409Conflict);
}
