using FluentValidation;

namespace NomoAI.API.Features.Plans.DeletePlan
{
    public class DeletePlanCommandValidator : AbstractValidator<DeletePlanCommand>
    {
        public DeletePlanCommandValidator()
        {
            RuleFor(x => x.PlanId)
                .GreaterThan(0)
                .WithMessage("PlanId must be greater than 0.");
        }
    }
}
