using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Support.GetMySupportTickets;

public sealed record GetMySupportTicketsResponse(
    IReadOnlyList<MySupportTicketItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record MySupportTicketItemResponse(
    int Id,
    string Subject,
    string Message,
    SupportTicketStatus Status,
    DateTime CreatedAt,
    string? AdminNote,
    DateTime? HandledAt,
    bool HasAdminNote);
