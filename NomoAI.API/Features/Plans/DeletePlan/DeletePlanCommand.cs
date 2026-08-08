using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Plans.DeletePlan
{
    public record DeletePlanCommand(int PlanId) : IRequest<Result>;
}
