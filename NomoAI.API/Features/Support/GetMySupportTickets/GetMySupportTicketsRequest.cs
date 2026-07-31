namespace NomoAI.API.Features.Support.GetMySupportTickets;

public sealed class GetMySupportTicketsRequest
{
    public int? PageNumber { get; init; } = 1;

    public int? PageSize { get; init; } = 10;
}
