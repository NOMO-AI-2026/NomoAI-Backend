using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Admin.Support.DeleteSupportTicket;

internal sealed class DeleteSupportTicketHandler
    : IRequestHandler<DeleteSupportTicketCommand, Result>
{
    private readonly AppDbContext _dbContext;

    public DeleteSupportTicketHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(
        DeleteSupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.SupportTickets
            .Where(item =>
                item.Id == request.Id &&
                !item.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(AdminSupportErrors.TicketNotFound);
        }

        ticket.IsDeleted = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
