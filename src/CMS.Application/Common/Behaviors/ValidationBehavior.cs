using CMS.Shared.Results;
using FluentValidation;
using MediatR;

namespace CMS.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behaviour that runs all FluentValidation validators for a
/// request before its handler. On failure it short-circuits with a failed
/// <see cref="Result"/>/<see cref="Result{T}"/> (rather than throwing), so the
/// Result-based contract flows cleanly back to controllers and the API.
/// Requests whose response is not a <see cref="Result"/> are validated by throwing.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        var error = Error.Validation(
            "Validation.Failed",
            string.Join(" ", failures.Select(f => f.ErrorMessage).Distinct()));

        if (TryCreateFailureResult(error, out var failureResult))
        {
            return failureResult;
        }

        // Response is not a Result type — surface as an exception.
        throw new ValidationException(failures);
    }

    private static bool TryCreateFailureResult(Error error, out TResponse response)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            response = (TResponse)(object)Result.Failure(error);
            return true;
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
                .MakeGenericMethod(valueType);

            response = (TResponse)failureMethod.Invoke(null, [error])!;
            return true;
        }

        response = default!;
        return false;
    }
}
