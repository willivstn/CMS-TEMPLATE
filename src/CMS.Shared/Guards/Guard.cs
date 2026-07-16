using System.Runtime.CompilerServices;

namespace CMS.Shared.Guards;

/// <summary>
/// Lightweight argument-validation helpers that throw on invalid input.
/// Use for enforcing invariants at the boundaries of domain objects and services.
/// The <c>[CallerArgumentExpression]</c> attribute captures the argument name
/// automatically, so callers rarely need to pass <c>paramName</c>.
/// </summary>
public static class Guard
{
    /// <summary>Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null.</summary>
    public static T AgainstNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value ?? throw new ArgumentNullException(paramName);

    /// <summary>Throws when a string is null, empty, or whitespace.</summary>
    public static string AgainstNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        }

        return value;
    }

    /// <summary>Throws when a value is negative.</summary>
    public static int AgainstNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value < 0 ? throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.") : value;

    /// <summary>Throws when an enumerable is null or contains no elements.</summary>
    public static IReadOnlyCollection<T> AgainstNullOrEmpty<T>(
        IReadOnlyCollection<T>? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        AgainstNull(value, paramName);
        if (value!.Count == 0)
        {
            throw new ArgumentException("Collection cannot be empty.", paramName);
        }

        return value;
    }
}
