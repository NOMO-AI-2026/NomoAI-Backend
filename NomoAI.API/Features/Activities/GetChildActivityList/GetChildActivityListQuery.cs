using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Activities.GetChildActivityList
{
    public record GetChildActivityListQuery(int ChildId) : IRequest<Result<IEnumerable<ActivityResponseDto>>>;
}
