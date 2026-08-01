namespace NomoAI.API.Features.Support.UpdateMySupportTicket;

public sealed record UpdateMySupportTicketRequest(
    string Subject,
    string Message);
