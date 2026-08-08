using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Plans.GetAllPlans
{
    public record GetAllPlansQuery() : IRequest<Result<IEnumerable<PlanResponse>>>;
}
