using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.GetChildDetails
{
    public record GetChildDeatilsQuery(string UserId, int ChildId, string? Role) : IRequest<Result<ChildDeatailsResponse>>;
}
