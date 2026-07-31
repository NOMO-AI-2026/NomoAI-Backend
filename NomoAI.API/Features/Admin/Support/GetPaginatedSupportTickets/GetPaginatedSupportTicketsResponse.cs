using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Admin.Support.GetPaginatedSupportTickets;

public sealed record GetPaginatedSupportTicketsResponse(
    IReadOnlyList<SupportTicketListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record SupportTicketListItemResponse(
    int Id,
    string Subject,
    SupportTicketStatus Status,
    DateTime CreatedAt,
    string UserId,
    string UserFullName,
    string UserEmail,
    string? UserRole,
    bool HasAdminNote);
