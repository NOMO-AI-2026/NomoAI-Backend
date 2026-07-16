using FluentValidation;

namespace NomoAI.API.Features.Parents.SearchParents;

public sealed class SearchParentsValidator
    : AbstractValidator<SearchParentsQuery>
{
    public SearchParentsValidator()
    {
        RuleFor(query => query.SearchTerm)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage("Search term is required.")
            .MinimumLength(2)
            .WithMessage("Search term must contain at least 2 characters.")
            .MaximumLength(100)
            .WithMessage("Search term cannot exceed 100 characters.");

        RuleFor(query => query.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than zero.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("Page size must be between 1 and 50.");
    }
}