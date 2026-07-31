using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Support.GetMySupportTickets;

public sealed record GetMySupportTicketsQuery(
    string UserId,
    int PageNumber,
    int PageSize)
    : IRequest<Result<GetMySupportTicketsResponse>>;
