using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Admin.Support.HandleSupportTicket;

public sealed record HandleSupportTicketResponse(
    int Id,
    string Subject,
    SupportTicketStatus Status,
    string? AdminNote,
    string? HandledByAdminUserId,
    DateTime? HandledAt);
