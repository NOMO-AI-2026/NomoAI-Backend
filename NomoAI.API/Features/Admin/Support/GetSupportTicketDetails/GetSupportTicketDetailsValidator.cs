using FluentValidation;

namespace NomoAI.API.Features.Admin.Support.GetSupportTicketDetails;

public sealed class GetSupportTicketDetailsValidator
    : AbstractValidator<GetSupportTicketDetailsQuery>
{
    public GetSupportTicketDetailsValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Ticket ID must be greater than zero.");
    }
}
