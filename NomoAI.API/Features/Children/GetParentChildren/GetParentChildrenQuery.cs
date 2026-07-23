using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.GetParentChildren
{
    public record GetParentChildrenQuery(string UserId) : IRequest<Result<IEnumerable<ChildrenResponse>>>;
}
