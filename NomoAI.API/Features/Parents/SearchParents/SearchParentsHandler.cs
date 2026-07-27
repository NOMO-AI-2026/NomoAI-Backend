using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Parents.SearchParents;

internal sealed class SearchParentsHandler
    : IRequestHandler<
        SearchParentsQuery,
        Result<SearchParentsResponse>>
{
    private readonly AppDbContext _dbContext;

    public SearchParentsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<SearchParentsResponse>> Handle(
        SearchParentsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Parent> query =
            BuildSearchQuery(request.SearchTerm);

        int totalCount =
            await query.CountAsync(cancellationToken);

        List<ParentSearchItemResponse> parents =
            await GetPageAsync(
                query,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

        SearchParentsResponse response =
            CreateResponse(
                parents,
                request.PageNumber,
                request.PageSize,
                totalCount);

        return Result.Success(response);
    }

    private IQueryable<Parent> BuildSearchQuery(
        string searchTerm)
    {
        string normalizedSearchTerm =
            searchTerm.Trim();

        string searchPattern =
            $"%{normalizedSearchTerm}%";

        return _dbContext.Parents
            .AsNoTracking()
            .Where(parent =>
                !parent.User.IsDeleted && !parent.IsDeleted && 
                (
                    EF.Functions.Like(
                        parent.User.Fullname,
                        searchPattern) ||

                    (
                        parent.User.Email != null &&
                        EF.Functions.Like(
                            parent.User.Email,
                            searchPattern)
                    ) ||

                    (
                        parent.User.PhoneNumber != null &&
                        EF.Functions.Like(
                            parent.User.PhoneNumber,
                            searchPattern)
                    )
                ));
    }

    private static async Task<List<ParentSearchItemResponse>>
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
                new ParentSearchItemResponse(
                    parent.Id,
                    parent.UserId,
                    parent.User.Fullname,
                    parent.User.Email ?? string.Empty,
                    parent.User.PhoneNumber))
            .ToListAsync(cancellationToken);
    }

    private static SearchParentsResponse CreateResponse(
        IReadOnlyList<ParentSearchItemResponse> parents,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        int totalPages =
            CalculateTotalPages(
                totalCount,
                pageSize);

        return new SearchParentsResponse(
            parents,
            pageNumber,
            pageSize,
            totalCount,
            totalPages);
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