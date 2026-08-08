using FluentValidation;

namespace NomoAI.API.Features.Plans.AddPlan
{
    public class AddPlanCommandValidator : AbstractValidator<AddPlanCommand>
    {
        public AddPlanCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleFor(x => x.IncludedMinutes)
                .GreaterThanOrEqualTo(0).WithMessage("IncludedMinutes must be greater than or equal to 0.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.");

            RuleFor(x => x.Currency)
                .IsInEnum().WithMessage("Currency is invalid.");
        }
    }
}
