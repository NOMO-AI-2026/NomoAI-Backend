using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.GetParentChildren
{
    public record GetParentChildrenQuery(string UserId, string? Name, int? PageNumber, int? PageSize) : IRequest<Result<PaginatedList<ChildrenResponse>>>;
}
