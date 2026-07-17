using FluentValidation;
using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IValidator<TRequest>[] _validators;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators.ToArray();
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Length == 0)
        {
            return await next();
        }

        var validationContext =
            new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(validator =>
                validator.ValidateAsync(
                    validationContext,
                    cancellationToken)));

        var validationFailures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToArray();

        if (validationFailures.Length == 0)
        {
            return await next();
        }

        Error validationError =
            ValidationErrors.FromValidationFailures(
                validationFailures);

        return ResultResponseFactory
            .CreateFailure<TResponse>(validationError);
    }
}