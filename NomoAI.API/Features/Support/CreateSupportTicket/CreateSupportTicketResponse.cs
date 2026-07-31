using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Support.CreateSupportTicket;

public sealed record CreateSupportTicketResponse(
    int Id,
    string Subject,
    string Message,
    SupportTicketStatus Status,
    DateTime CreatedAt);
