using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Support.UpdateMySupportTicket;

public sealed record UpdateMySupportTicketResponse(
    int Id,
    string Subject,
    string Message,
    SupportTicketStatus Status,
    DateTime CreatedAt,
    string? AdminNote,
    DateTime? HandledAt);
