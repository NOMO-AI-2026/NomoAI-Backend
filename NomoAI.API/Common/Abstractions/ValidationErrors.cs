using FluentValidation.Results;

namespace NomoAI.API.Common.Abstractions;

public static class ValidationErrors
{
    public static Error FromValidationFailures(
        IEnumerable<ValidationFailure> failures)
    {
        string description = string.Join(
            " | ",
            failures
                .GroupBy(failure =>
                    string.IsNullOrWhiteSpace(failure.PropertyName)
                        ? "Request"
                        : failure.PropertyName)
                .Select(group =>
                    $"{group.Key}: {string.Join(
                        ", ",
                        group
                            .Select(failure => failure.ErrorMessage)
                            .Distinct())}"));

        return new Error(
            "Validation.Error",
            description,
            StatusCodes.Status400BadRequest);
    }
}