using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Support.CreateSupportTicket;

public sealed record CreateSupportTicketCommand(
    string UserId,
    string Subject,
    string Message)
    : IRequest<Result<CreateSupportTicketResponse>>;
