using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Support;

public static class SupportTicketRules
{
    public static bool CanUserModify(SupportTicket ticket)
    {
        return CanUserModify(
            ticket.Status,
            ticket.HandledAt,
            ticket.HandledByAdminUserId,
            ticket.IsDeleted);
    }

    public static bool CanUserModify(
        SupportTicketStatus status,
        DateTime? handledAt,
        string? handledByAdminUserId,
        bool isDeleted = false)
    {
        return !isDeleted
            && status == SupportTicketStatus.Open
            && handledAt is null
            && handledByAdminUserId is null;
    }
}
