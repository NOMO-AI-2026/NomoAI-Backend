using System.Reflection;

namespace NomoAI.API.Common.Abstractions;

internal static class ResultResponseFactory
{
    public static TResponse CreateFailure<TResponse>(
        Error error)
    {
        Type responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() ==
            typeof(Result<>))
        {
            Type valueType =
                responseType.GetGenericArguments()[0];

            MethodInfo genericFailureMethod =
                typeof(Result)
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Static)
                    .Single(method =>
                        method.Name == nameof(Result.Failure) &&
                        method.IsGenericMethodDefinition &&
                        method.GetGenericArguments().Length == 1 &&
                        method.GetParameters().Length == 1 &&
                        method.GetParameters()[0].ParameterType ==
                        typeof(Error));

            object failureResult = genericFailureMethod
                .MakeGenericMethod(valueType)
                .Invoke(
                    null,
                    new object[] { error })!;

            return (TResponse)failureResult;
        }

        throw new InvalidOperationException(
            $"The response type '{responseType.Name}' is not supported. " +
            "Validated MediatR requests must return Result or Result<T>.");
    }
}