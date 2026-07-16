using FluentValidation;

namespace NomoAI.API.Features.Children.AssignChildToParent;

public sealed class AssignChildToParentValidator
    : AbstractValidator<AssignChildToParentCommand>
{
    public AssignChildToParentValidator()
    {
        RuleFor(command => command.ChildId)
            .GreaterThan(0)
            .WithMessage("Child ID must be greater than zero.");

        //RuleFor(command => command.ParentId)
        //    .GreaterThan(0)
        //    .WithMessage("Parent ID must be greater than zero.");

        RuleFor(command => command.DoctorUserId)
            .NotEmpty()
            .WithMessage("Authenticated doctor ID is required.");
    }
}