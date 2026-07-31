using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Admin.Support.DeleteSupportTicket;

public sealed record DeleteSupportTicketCommand(int Id)
    : IRequest<Result>;
