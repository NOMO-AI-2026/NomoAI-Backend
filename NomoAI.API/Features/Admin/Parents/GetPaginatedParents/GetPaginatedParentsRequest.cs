namespace NomoAI.API.Features.Admin.Parents.GetPaginatedParents;

public sealed class GetPaginatedParentsRequest
{
    public int? PageNumber { get; init; } = 1;

    public int? PageSize { get; init; } = 10;
}