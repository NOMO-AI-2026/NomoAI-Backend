using FluentValidation;

namespace NomoAI.API.Features.Support.UpdateMySupportTicket;

public sealed class UpdateMySupportTicketValidator
    : AbstractValidator<UpdateMySupportTicketCommand>
{
    public UpdateMySupportTicketValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("Ticket ID must be greater than zero.");

        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("Authenticated user ID is required.");

        RuleFor(command => command.Subject)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage("Subject is required.")
            .MinimumLength(5)
            .WithMessage("Subject must be at least 5 characters.")
            .MaximumLength(150)
            .WithMessage("Subject cannot exceed 150 characters.");

        RuleFor(command => command.Message)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage("Message is required.")
            .MinimumLength(10)
            .WithMessage("Message must be at least 10 characters.")
            .MaximumLength(2000)
            .WithMessage("Message cannot exceed 2000 characters.");
    }
}
