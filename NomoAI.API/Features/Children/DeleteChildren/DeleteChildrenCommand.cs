using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.DeleteChildren
{
    public record DeleteChildrenCommand(int ChildId) : IRequest<Result<bool>>;
}
