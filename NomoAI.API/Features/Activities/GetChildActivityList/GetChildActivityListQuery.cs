using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Activities.GetChildActivityList
{
    /// <param name="OnlyAvailableForSession">
    /// When true, returns only activities that can still start a therapy session.
    /// When false/omitted, returns all non-deleted activities (history / manage UI).
    /// </param>
    public record GetChildActivityListQuery(int ChildId, bool? OnlyAvailableForSession)
        : IRequest<Result<IEnumerable<ActivityResponseDto>>>;
}
