using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Admin.Parents.GetPaginatedParents;

public sealed record GetPaginatedParentsQuery(
    int PageNumber,
    int PageSize)
    : IRequest<Result<GetPaginatedParentsResponse>>;