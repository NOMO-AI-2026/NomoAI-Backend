using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Support.GetMySupportTickets;

internal sealed class GetMySupportTicketsHandler
    : IRequestHandler<
        GetMySupportTicketsQuery,
        Result<GetMySupportTicketsResponse>>
{
    private readonly AppDbContext _dbContext;

    public GetMySupportTicketsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetMySupportTicketsResponse>> Handle(
        GetMySupportTicketsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<SupportTicket> query =
            _dbContext.SupportTickets
                .AsNoTracking()
                .Where(ticket =>
                    ticket.UserId == request.UserId &&
                    !ticket.IsDeleted);

        int totalCount =
            await query.CountAsync(cancellationToken);

        int recordsToSkip =
            (request.PageNumber - 1) * request.PageSize;

        List<MySupportTicketItemResponse> items =
            await query
                .OrderByDescending(ticket => ticket.CreatedAt)
                .ThenByDescending(ticket => ticket.Id)
                .Skip(recordsToSkip)
                .Take(request.PageSize)
                .Select(ticket =>
                    new MySupportTicketItemResponse(
                        ticket.Id,
                        ticket.Subject,
                        ticket.Message,
                        ticket.Status,
                        ticket.CreatedAt,
                        ticket.AdminNote,
                        ticket.HandledAt,
                        ticket.AdminNote != null &&
                        ticket.AdminNote != string.Empty))
                .ToListAsync(cancellationToken);

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount / (double)request.PageSize);

        var response = new GetMySupportTicketsResponse(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages,
            HasPreviousPage: request.PageNumber > 1,
            HasNextPage: request.PageNumber < totalPages);

        return Result.Success(response);
    }
}
