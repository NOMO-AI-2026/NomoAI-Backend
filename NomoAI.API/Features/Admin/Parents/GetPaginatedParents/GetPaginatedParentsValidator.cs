using FluentValidation;

namespace NomoAI.API.Features.Admin.Parents.GetPaginatedParents;

public sealed class GetPaginatedParentsValidator
    : AbstractValidator<GetPaginatedParentsQuery>
{
    public GetPaginatedParentsValidator()
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
    }
}