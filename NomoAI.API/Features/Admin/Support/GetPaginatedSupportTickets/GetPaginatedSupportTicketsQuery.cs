using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Admin.Support.GetPaginatedSupportTickets;

public sealed record GetPaginatedSupportTicketsQuery(
    int PageNumber,
    int PageSize,
    SupportTicketStatus? Status,
    string? Name)
    : IRequest<Result<GetPaginatedSupportTicketsResponse>>;
