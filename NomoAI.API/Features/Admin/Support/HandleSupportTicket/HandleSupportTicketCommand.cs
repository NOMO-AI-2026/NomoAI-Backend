using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Admin.Support.HandleSupportTicket;

public sealed record HandleSupportTicketCommand(
    int Id,
    SupportTicketStatus Status,
    string? AdminNote,
    string AdminUserId)
    : IRequest<Result<HandleSupportTicketResponse>>;
