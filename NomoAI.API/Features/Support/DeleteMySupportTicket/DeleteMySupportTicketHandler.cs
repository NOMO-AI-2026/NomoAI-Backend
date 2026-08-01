using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Support.DeleteMySupportTicket;

internal sealed class DeleteMySupportTicketHandler
    : IRequestHandler<DeleteMySupportTicketCommand, Result>
{
    private readonly AppDbContext _dbContext;

    public DeleteMySupportTicketHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(
        DeleteMySupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.SupportTickets
            .Where(item =>
                item.Id == request.Id &&
                !item.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(SupportErrors.TicketNotFound);
        }

        if (!string.Equals(
                ticket.UserId,
                request.UserId,
                StringComparison.Ordinal))
        {
            return Result.Failure(SupportErrors.Forbidden);
        }

        if (!SupportTicketRules.CanUserModify(ticket))
        {
            return Result.Failure(SupportErrors.TicketLockedByAdmin);
        }

        ticket.IsDeleted = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
