using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Admin.Support.HandleSupportTicket;

public sealed record HandleSupportTicketRequest(
    SupportTicketStatus Status,
    string? AdminNote);
