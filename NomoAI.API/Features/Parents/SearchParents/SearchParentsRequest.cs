namespace NomoAI.API.Features.Parents.SearchParents;

public sealed class SearchParentsRequest
{
    public string SearchTerm { get; init; } = string.Empty;

    public int? PageNumber { get; init; } = 1;

    public int? PageSize { get; init; } = 10;
}