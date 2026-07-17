using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.GetDoctorChildren
{
    public record GetDoctorChildrenQuery(string UserId) : IRequest<Result<IEnumerable<ChildrenResponse>>>;
}
