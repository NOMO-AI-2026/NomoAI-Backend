using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Parents.SearchParents;

internal sealed class SearchParentsHandler
    : IRequestHandler<SearchParentsQuery, Result<SearchParentsResponse>>
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
        string searchTerm = request.SearchTerm.Trim();
        string searchPattern = $"%{searchTerm}%";

        IQueryable<Parent> parentsQuery = _dbContext.Parents
            .AsNoTracking()
            .Where(parent =>
                !parent.User.IsDeleted &&
                (
                    EF.Functions.Like(
                        parent.User.Fullname,
                        searchPattern) ||

                    (parent.User.Email != null &&
                     EF.Functions.Like(
                         parent.User.Email,
                         searchPattern)) ||

                    (parent.User.PhoneNumber != null &&
                     EF.Functions.Like(
                         parent.User.PhoneNumber,
                         searchPattern))
                ));

        int totalCount = await parentsQuery.CountAsync(
            cancellationToken);

        List<ParentSearchItemResponse> parents =
            await parentsQuery
                .OrderBy(parent => parent.User.Fullname)
                .ThenBy(parent => parent.Id)
                .Skip(
                    (request.PageNumber - 1) *
                    request.PageSize)
                .Take(request.PageSize)
                .Select(parent => new ParentSearchItemResponse(
                    parent.Id,
                    parent.UserId,
                    parent.User.Fullname,
                    parent.User.Email ?? string.Empty,
                    parent.User.PhoneNumber))
                .ToListAsync(cancellationToken);

        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)request.PageSize);

        var response = new SearchParentsResponse(
            parents,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages);

        return Result.Success(response);
    }
}