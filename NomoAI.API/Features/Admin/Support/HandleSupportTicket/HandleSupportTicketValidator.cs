using FluentValidation;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Admin.Support.HandleSupportTicket;

public sealed class HandleSupportTicketValidator
    : AbstractValidator<HandleSupportTicketCommand>
{
    public HandleSupportTicketValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("Ticket ID must be greater than zero.");

        RuleFor(command => command.AdminUserId)
            .NotEmpty()
            .WithMessage("Authenticated admin ID is required.");

        RuleFor(command => command.Status)
            .IsInEnum()
            .WithMessage("Status is invalid.")
            .Must(status => status != SupportTicketStatus.Open)
            .WithMessage(
                "Status cannot be set back to Open. Use InProgress, Resolved, or Closed.");

        RuleFor(command => command.AdminNote)
            .MaximumLength(1000)
            .When(command => command.AdminNote is not null)
            .WithMessage("Admin note cannot exceed 1000 characters.");
    }
}
