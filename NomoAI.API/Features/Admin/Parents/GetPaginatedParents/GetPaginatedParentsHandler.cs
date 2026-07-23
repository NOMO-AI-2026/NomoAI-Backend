using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Admin.Parents.GetPaginatedParents;

internal sealed class GetPaginatedParentsHandler
    : IRequestHandler<
        GetPaginatedParentsQuery,
        Result<GetPaginatedParentsResponse>>
{
    private readonly AppDbContext _dbContext;

    public GetPaginatedParentsHandler(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetPaginatedParentsResponse>> Handle(
        GetPaginatedParentsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Parent> query =
            BuildParentsQuery();

        int totalCount =
            await query.CountAsync(cancellationToken);

        List<ParentListItemResponse> parents =
            await GetPageAsync(
                query,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

        GetPaginatedParentsResponse response =
            CreateResponse(
                parents,
                request.PageNumber,
                request.PageSize,
                totalCount);

        return Result.Success(response);
    }

    private IQueryable<Parent> BuildParentsQuery()
    {
        return _dbContext.Parents
            .AsNoTracking()
            .Where(parent =>
                !parent.User.IsDeleted);
    }

    private static async Task<List<ParentListItemResponse>>
        GetPageAsync(
            IQueryable<Parent> query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        int recordsToSkip =
            (pageNumber - 1) * pageSize;

        return await query
            .OrderBy(parent => parent.User.Fullname)
            .ThenBy(parent => parent.Id)
            .Skip(recordsToSkip)
            .Take(pageSize)
            .Select(parent =>
                new ParentListItemResponse(
                    parent.Id,
                    parent.UserId,
                    parent.User.Fullname,
                    parent.User.Email ?? string.Empty,
                    parent.User.PhoneNumber))
            .ToListAsync(cancellationToken);
    }

    private static GetPaginatedParentsResponse CreateResponse(
        IReadOnlyList<ParentListItemResponse> parents,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        int totalPages =
            CalculateTotalPages(
                totalCount,
                pageSize);

        return new GetPaginatedParentsResponse(
            parents,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            HasPreviousPage: pageNumber > 1,
            HasNextPage: pageNumber < totalPages);
    }

    private static int CalculateTotalPages(
        int totalCount,
        int pageSize)
    {
        return totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)pageSize);
    }
}