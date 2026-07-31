using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Admin.Support.GetSupportTicketDetails;

public sealed record GetSupportTicketDetailsResponse(
    int Id,
    string Subject,
    string Message,
    SupportTicketStatus Status,
    string? AdminNote,
    string? HandledByAdminUserId,
    DateTime? HandledAt,
    DateTime CreatedAt,
    string UserId,
    string UserFullName,
    string UserEmail,
    string? UserRole);
