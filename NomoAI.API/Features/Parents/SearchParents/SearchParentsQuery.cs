using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Parents.SearchParents;

public sealed record SearchParentsQuery(
    string SearchTerm,
    int PageNumber,
    int PageSize)
    : IRequest<Result<SearchParentsResponse>>;