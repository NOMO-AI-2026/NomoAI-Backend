using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Admin.Support.GetSupportTicketDetails;

internal sealed class GetSupportTicketDetailsHandler
    : IRequestHandler<
        GetSupportTicketDetailsQuery,
        Result<GetSupportTicketDetailsResponse>>
{
    private readonly AppDbContext _dbContext;

    public GetSupportTicketDetailsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetSupportTicketDetailsResponse>> Handle(
        GetSupportTicketDetailsQuery request,
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
                item.Subject,
                item.Message,
                item.Status,
                item.AdminNote,
                item.HandledByAdminUserId,
                item.HandledAt,
                item.CreatedAt,
                item.UserId,
                UserFullName = item.User.Fullname,
                UserEmail = item.User.Email ?? string.Empty
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<GetSupportTicketDetailsResponse>(
                AdminSupportErrors.TicketNotFound);
        }

        string? userRole = await (
                from userRoleEntry in _dbContext.UserRoles.AsNoTracking()
                join role in _dbContext.Roles.AsNoTracking()
                    on userRoleEntry.RoleId equals role.Id
                where userRoleEntry.UserId == ticket.UserId
                select role.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var response = new GetSupportTicketDetailsResponse(
            ticket.Id,
            ticket.Subject,
            ticket.Message,
            ticket.Status,
            ticket.AdminNote,
            ticket.HandledByAdminUserId,
            ticket.HandledAt,
            ticket.CreatedAt,
            ticket.UserId,
            ticket.UserFullName,
            ticket.UserEmail,
            userRole);

        return Result.Success(response);
    }
}
