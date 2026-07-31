using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Admin.Support.HandleSupportTicket;

internal sealed class HandleSupportTicketHandler
    : IRequestHandler<
        HandleSupportTicketCommand,
        Result<HandleSupportTicketResponse>>
{
    private readonly AppDbContext _dbContext;

    public HandleSupportTicketHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<HandleSupportTicketResponse>> Handle(
        HandleSupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Status == SupportTicketStatus.Open)
        {
            return Result.Failure<HandleSupportTicketResponse>(
                AdminSupportErrors.InvalidStatusTransition);
        }

        var ticket = await _dbContext.SupportTickets
            .Where(item =>
                item.Id == request.Id &&
                !item.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<HandleSupportTicketResponse>(
                AdminSupportErrors.TicketNotFound);
        }

        ticket.Status = request.Status;
        ticket.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote)
            ? ticket.AdminNote
            : request.AdminNote.Trim();
        ticket.HandledByAdminUserId = request.AdminUserId;
        ticket.HandledAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new HandleSupportTicketResponse(
            ticket.Id,
            ticket.Subject,
            ticket.Status,
            ticket.AdminNote,
            ticket.HandledByAdminUserId,
            ticket.HandledAt);

        return Result.Success(response);
    }
}
