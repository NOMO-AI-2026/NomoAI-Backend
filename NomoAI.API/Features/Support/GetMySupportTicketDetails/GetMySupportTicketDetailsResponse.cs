using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Support.GetMySupportTicketDetails;

public sealed record GetMySupportTicketDetailsResponse(
    int Id,
    string Subject,
    string Message,
    SupportTicketStatus Status,
    string? AdminNote,
    DateTime? HandledAt,
    DateTime CreatedAt);
