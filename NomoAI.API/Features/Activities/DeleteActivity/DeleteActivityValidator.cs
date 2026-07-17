using FluentValidation;

namespace NomoAI.API.Features.Activities.DeleteActivity;

public sealed class DeleteActivityValidator
    : AbstractValidator<DeleteActivityCommand>
{
    public DeleteActivityValidator()
    {
        RuleFor(command => command.ActivityId)
            .GreaterThan(0)
            .WithMessage("Activity ID must be greater than zero.");

        RuleFor(command => command.DoctorUserId)
            .NotEmpty()
            .WithMessage("Authenticated doctor ID is required.");
    }
}