using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.UpdateChildData
{
    public record UpdateChildCommand(int ChildId) : IRequest<Result<bool>>
    {
        public UpdateChildRequest Request { get; set; } = new UpdateChildRequest();
    }
}
