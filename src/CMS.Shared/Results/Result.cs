using System.Diagnostics.CodeAnalysis;

namespace CMS.Shared.Results;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail without
/// throwing. Carries an <see cref="Error"/> on failure. Use the generic
/// <see cref="Result{TValue}"/> when a value is returned on success.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot contain an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>True when the operation completed successfully.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The error describing the failure, or <see cref="Error.None"/> on success.</summary>
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>Convenience factory returning success or failure based on <paramref name="condition"/>.</summary>
    public static Result Create(bool condition, Error error) => condition ? Success() : Failure(error);
}

/// <summary>
/// A <see cref="Result"/> that carries a value on success.
/// </summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// The success value. Accessing it on a failed result throws — check
    /// <see cref="Result.IsSuccess"/> first, or use <see cref="Match{TOut}"/>.
    /// </summary>
    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    /// <summary>Implicitly wraps a value in a successful result.</summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>Implicitly wraps an error in a failed result.</summary>
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    /// <summary>Projects the result into a single value regardless of outcome.</summary>
    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(Error);
}
