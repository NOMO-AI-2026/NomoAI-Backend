using FluentValidation;

namespace NomoAI.API.Features.Support.GetMySupportTickets;

public sealed class GetMySupportTicketsValidator
    : AbstractValidator<GetMySupportTicketsQuery>
{
    public GetMySupportTicketsValidator()
    {
        RuleFor(query => query.UserId)
            .NotEmpty()
            .WithMessage("Authenticated user ID is required.");

        RuleFor(query => query.PageNumber)
            .GreaterThan(0)
            .WithMessage(
                "Page number must be greater than zero.");

        RuleFor(query => query.PageSize)
            .GreaterThan(0)
            .WithMessage(
                "Page size must be greater than zero.")
            .LessThanOrEqualTo(50)
            .WithMessage(
                "Page size cannot exceed 50.");
    }
}
