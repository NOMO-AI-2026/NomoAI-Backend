using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Support.GetMySupportTicketDetails;

internal sealed class GetMySupportTicketDetailsHandler
    : IRequestHandler<
        GetMySupportTicketDetailsQuery,
        Result<GetMySupportTicketDetailsResponse>>
{
    private readonly AppDbContext _dbContext;

    public GetMySupportTicketDetailsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetMySupportTicketDetailsResponse>> Handle(
        GetMySupportTicketDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.SupportTickets
            .AsNoTracking()
            .Where(item =>
                item.Id == request.Id &&
                !item.IsDeleted)
            .Select(item => new
            {
                item.Id,
                item.UserId,
                item.Subject,
                item.Message,
                item.Status,
                item.AdminNote,
                item.HandledAt,
                item.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<GetMySupportTicketDetailsResponse>(
                SupportErrors.TicketNotFound);
        }

        if (!string.Equals(
                ticket.UserId,
                request.UserId,
                StringComparison.Ordinal))
        {
            return Result.Failure<GetMySupportTicketDetailsResponse>(
                SupportErrors.Forbidden);
        }

        var response = new GetMySupportTicketDetailsResponse(
            ticket.Id,
            ticket.Subject,
            ticket.Message,
            ticket.Status,
            ticket.AdminNote,
            ticket.HandledAt,
            ticket.CreatedAt);

        return Result.Success(response);
    }
}
