using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Admin.Support;

public static class AdminSupportErrors
{
    public static readonly Error TicketNotFound = new(
        "Support.TicketNotFound",
        "Support ticket was not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error InvalidStatusTransition = new(
        "Support.InvalidStatusTransition",
        "Cannot set support ticket status back to Open.",
        StatusCodes.Status400BadRequest);
}
