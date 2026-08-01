using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Support.UpdateMySupportTicket;

public sealed record UpdateMySupportTicketCommand(
    int Id,
    string UserId,
    string Subject,
    string Message)
    : IRequest<Result<UpdateMySupportTicketResponse>>;
