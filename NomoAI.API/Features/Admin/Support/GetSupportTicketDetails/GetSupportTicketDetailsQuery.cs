using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Admin.Support.GetSupportTicketDetails;

public sealed record GetSupportTicketDetailsQuery(int Id)
    : IRequest<Result<GetSupportTicketDetailsResponse>>;
