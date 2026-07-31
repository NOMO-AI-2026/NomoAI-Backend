using FluentValidation;

namespace NomoAI.API.Features.Admin.Support.DeleteSupportTicket;

public sealed class DeleteSupportTicketValidator
    : AbstractValidator<DeleteSupportTicketCommand>
{
    public DeleteSupportTicketValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("Ticket ID must be greater than zero.");
    }
}
