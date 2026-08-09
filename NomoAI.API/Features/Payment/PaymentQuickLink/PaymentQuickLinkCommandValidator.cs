using FluentValidation;

namespace NomoAI.API.Features.Payment.PaymentQuickLink
{
    public class PaymentQuickLinkCommandValidator : AbstractValidator<PaymentQuickLinkCommand>
    {
        public PaymentQuickLinkCommandValidator()
        {
            RuleFor(x => x.DoctorUserId)
                .NotEmpty()
                .WithMessage("Authenticated doctor ID is required.");

            RuleFor(x => x.PaymentMethodId)
                .NotEmpty()
                .WithMessage("PaymentMethodId is required.");

            RuleFor(x => x.PlanId)
                .GreaterThan(0)
                .WithMessage("PlanId must be greater than 0.");

            RuleFor(x => x.Idempotency)
                .NotEmpty()
                .WithMessage("Idempotency is required.")
                .MaximumLength(100)
                .WithMessage("Idempotency cannot exceed 100 characters.");
        }
    }
}
