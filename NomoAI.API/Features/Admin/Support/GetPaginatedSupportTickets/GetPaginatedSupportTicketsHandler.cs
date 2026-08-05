using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Admin.Support.GetPaginatedSupportTickets;

internal sealed class GetPaginatedSupportTicketsHandler
    : IRequestHandler<
        GetPaginatedSupportTicketsQuery,
        Result<GetPaginatedSupportTicketsResponse>>
{
    private readonly AppDbContext _dbContext;

    public GetPaginatedSupportTicketsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetPaginatedSupportTicketsResponse>> Handle(
        GetPaginatedSupportTicketsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<SupportTicket> query =
            _dbContext.SupportTickets
                .AsNoTracking()
                .Where(ticket => !ticket.IsDeleted);

        if (request.Status.HasValue)
        {
            query = query.Where(ticket =>
                ticket.Status == request.Status.Value);
        }
        if(request.Name != null)
        {
            query = query.Where(ticket =>
               ticket.User.Fullname.Contains(request.Name));
        }
        int totalCount =
            await query.CountAsync(cancellationToken);

        int recordsToSkip =
            (request.PageNumber - 1) * request.PageSize;

        var pageTickets = await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ThenByDescending(ticket => ticket.Id)
            .Skip(recordsToSkip)
            .Take(request.PageSize)
            .Select(ticket => new
            {
                ticket.Id,
                ticket.Subject,
                ticket.Status,
                ticket.CreatedAt,
                ticket.UserId,
                UserFullName = ticket.User.Fullname,
                UserEmail = ticket.User.Email ?? string.Empty,
                HasAdminNote =
                    ticket.AdminNote != null &&
                    ticket.AdminNote != string.Empty
            })
            .ToListAsync(cancellationToken);

        List<string> userIds = pageTickets
            .Select(ticket => ticket.UserId)
            .Distinct()
            .ToList();

        Dictionary<string, string?> rolesByUserId =
            await LoadPrimaryRolesAsync(userIds, cancellationToken);

        List<SupportTicketListItemResponse> items =
            pageTickets
                .Select(ticket =>
                    new SupportTicketListItemResponse(
                        ticket.Id,
                        ticket.Subject,
                        ticket.Status,
                        ticket.CreatedAt,
                        ticket.UserId,
                        ticket.UserFullName,
                        ticket.UserEmail,
                        rolesByUserId.GetValueOrDefault(ticket.UserId),
                        ticket.HasAdminNote))
                .ToList();

        int totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount / (double)request.PageSize);

        var response = new GetPaginatedSupportTicketsResponse(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages,
            HasPreviousPage: request.PageNumber > 1,
            HasNextPage: request.PageNumber < totalPages);

        return Result.Success(response);
    }

    private async Task<Dictionary<string, string?>> LoadPrimaryRolesAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<string, string?>();
        }

        var roles = await (
                from userRole in _dbContext.UserRoles.AsNoTracking()
                join role in _dbContext.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                select new
                {
                    userRole.UserId,
                    RoleName = role.Name
                })
            .ToListAsync(cancellationToken);

        return roles
            .GroupBy(role => role.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.RoleName).FirstOrDefault());
    }
}
