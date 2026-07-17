namespace NomoAI.API.Features.Parents.SearchParents;

public sealed record SearchParentsResponse(
    IReadOnlyList<ParentSearchItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ParentSearchItemResponse(
    int ParentId,
    string UserId,
    string Fullname,
    string Email,
    string? PhoneNumber);