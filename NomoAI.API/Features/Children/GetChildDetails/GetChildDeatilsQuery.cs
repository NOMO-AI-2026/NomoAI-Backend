using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.GetChildDetails
{
    public record GetChildDeatilsQuery(int ChildId) : IRequest<Result<ChildDeatailsResponse>>;
}
