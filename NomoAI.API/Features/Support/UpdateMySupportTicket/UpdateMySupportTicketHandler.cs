using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Support.UpdateMySupportTicket;

internal sealed class UpdateMySupportTicketHandler
    : IRequestHandler<
        UpdateMySupportTicketCommand,
        Result<UpdateMySupportTicketResponse>>
{
    private readonly AppDbContext _dbContext;

    public UpdateMySupportTicketHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UpdateMySupportTicketResponse>> Handle(
        UpdateMySupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.SupportTickets
            .Where(item =>
                item.Id == request.Id &&
                !item.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<UpdateMySupportTicketResponse>(
                SupportErrors.TicketNotFound);
        }

        if (!string.Equals(
                ticket.UserId,
                request.UserId,
                StringComparison.Ordinal))
        {
            return Result.Failure<UpdateMySupportTicketResponse>(
                SupportErrors.Forbidden);
        }

        if (!SupportTicketRules.CanUserModify(ticket))
        {
            return Result.Failure<UpdateMySupportTicketResponse>(
                SupportErrors.TicketLockedByAdmin);
        }

        ticket.Subject = request.Subject.Trim();
        ticket.Message = request.Message.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new UpdateMySupportTicketResponse(
            ticket.Id,
            ticket.Subject,
            ticket.Message,
            ticket.Status,
            ticket.CreatedAt,
            ticket.AdminNote,
            ticket.HandledAt);

        return Result.Success(response);
    }
}
