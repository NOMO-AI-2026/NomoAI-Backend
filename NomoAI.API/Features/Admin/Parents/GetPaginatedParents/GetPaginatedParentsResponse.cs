namespace NomoAI.API.Features.Admin.Parents.GetPaginatedParents;

public sealed record GetPaginatedParentsResponse(
    IReadOnlyList<ParentListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record ParentListItemResponse(
    int ParentId,
    string UserId,
    string Fullname,
    string Email,
    string? PhoneNumber);