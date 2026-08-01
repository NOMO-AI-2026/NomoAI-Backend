using FluentValidation;

namespace NomoAI.API.Features.Support.DeleteMySupportTicket;

public sealed class DeleteMySupportTicketValidator
    : AbstractValidator<DeleteMySupportTicketCommand>
{
    public DeleteMySupportTicketValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("Ticket ID must be greater than zero.");

        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("Authenticated user ID is required.");
    }
}
