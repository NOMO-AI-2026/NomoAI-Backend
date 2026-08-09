using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Plans.AddPlan
{
    public record AddPlanCommand(
        string Name,
        string? Description,
        int IncludedMinutes,
        decimal Price,
        MoneyCurrency Currency) : IRequest<Result<int>>;
}
