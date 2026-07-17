using FluentValidation;

namespace NomoAI.API.Features.Activities.CreateActivity;

public sealed class CreateActivityValidator
    : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityValidator()
    {
        RuleFor(command => command.ChildId)
            .GreaterThan(0)
            .WithMessage("Child ID must be greater than zero.");

        RuleFor(command => command.ActivityTarget)
            .IsInEnum()
            .WithMessage("Activity target type is invalid.");

        RuleFor(command => command.Content)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage("Activity content is required.")
            .MaximumLength(500)
            .WithMessage(
                "Activity content cannot exceed 500 characters.");

        RuleFor(command => command.EstimatedDurationMinutes)
            .InclusiveBetween(1, 60)
            .WithMessage(
                "Estimated duration must be between 1 and 60 minutes.");

        RuleFor(command => command.DoctorUserId)
            .NotEmpty()
            .WithMessage("Authenticated doctor ID is required.");
    }
}