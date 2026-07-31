using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Support.GetMySupportTicketDetails;

public sealed record GetMySupportTicketDetailsQuery(
    int Id,
    string UserId)
    : IRequest<Result<GetMySupportTicketDetailsResponse>>;
