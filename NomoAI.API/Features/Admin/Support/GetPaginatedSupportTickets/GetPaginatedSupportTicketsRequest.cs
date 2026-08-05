using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Admin.Support.GetPaginatedSupportTickets;

public sealed class GetPaginatedSupportTicketsRequest
{
    public string ? Name { get; init; }
    public int? PageNumber { get; init; } = 1;

    public int? PageSize { get; init; } = 10;

    public SupportTicketStatus? Status { get; init; }
}
