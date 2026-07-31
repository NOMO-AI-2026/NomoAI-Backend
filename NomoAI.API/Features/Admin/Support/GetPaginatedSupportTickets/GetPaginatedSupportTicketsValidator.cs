using FluentValidation;

namespace NomoAI.API.Features.Admin.Support.GetPaginatedSupportTickets;

public sealed class GetPaginatedSupportTicketsValidator
    : AbstractValidator<GetPaginatedSupportTicketsQuery>
{
    public GetPaginatedSupportTicketsValidator()
    {
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

        RuleFor(query => query.Status)
            .IsInEnum()
            .When(query => query.Status.HasValue)
            .WithMessage("Status is invalid.");
    }
}
