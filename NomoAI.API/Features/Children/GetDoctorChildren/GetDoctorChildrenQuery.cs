using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.GetDoctorChildren
{
    public record GetDoctorChildrenQuery(string UserId, string? Name, int? PageNumber, int? PageSize) : IRequest<Result<PaginatedList<ChildrenResponse>>>;
}
