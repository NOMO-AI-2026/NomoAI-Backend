using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Support.DeleteMySupportTicket;

public sealed record DeleteMySupportTicketCommand(
    int Id,
    string UserId)
    : IRequest<Result>;
