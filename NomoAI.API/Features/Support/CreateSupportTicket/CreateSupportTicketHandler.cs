using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Support.CreateSupportTicket;

internal sealed class CreateSupportTicketHandler
    : IRequestHandler<
        CreateSupportTicketCommand,
        Result<CreateSupportTicketResponse>>
{
    private readonly AppDbContext _dbContext;

    public CreateSupportTicketHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CreateSupportTicketResponse>> Handle(
        CreateSupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        var ticket = new SupportTicket
        {
            UserId = request.UserId,
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Status = SupportTicketStatus.Open
        };

        _dbContext.SupportTickets.Add(ticket);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new CreateSupportTicketResponse(
            ticket.Id,
            ticket.Subject,
            ticket.Message,
            ticket.Status,
            ticket.CreatedAt);

        return Result.Success(response);
    }
}
