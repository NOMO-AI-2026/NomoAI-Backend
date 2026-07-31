namespace NomoAI.API.Features.Support.CreateSupportTicket;

public sealed record CreateSupportTicketRequest(
    string Subject,
    string Message);
