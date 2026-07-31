using FluentValidation;

namespace NomoAI.API.Features.Support.GetMySupportTicketDetails;

public sealed class GetMySupportTicketDetailsValidator
    : AbstractValidator<GetMySupportTicketDetailsQuery>
{
    public GetMySupportTicketDetailsValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Ticket ID must be greater than zero.");

        RuleFor(query => query.UserId)
            .NotEmpty()
            .WithMessage("Authenticated user ID is required.");
    }
}
