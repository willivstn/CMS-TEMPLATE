namespace CMS.Shared.Results;

/// <summary>
/// Classifies the nature of a failure so callers (and the API layer) can map it
/// to an appropriate transport result (HTTP status, ModelState, etc.).
/// </summary>
public enum ErrorType
{
    /// <summary>A generic, unexpected failure.</summary>
    Failure = 0,

    /// <summary>One or more inputs failed validation.</summary>
    Validation = 1,

    /// <summary>A requested resource was not found.</summary>
    NotFound = 2,

    /// <summary>A conflict with the current state (e.g. duplicate, concurrency).</summary>
    Conflict = 3,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized = 4,

    /// <summary>The caller is authenticated but lacks permission.</summary>
    Forbidden = 5,
}

/// <summary>
/// An immutable, structured description of a failure. Prefer returning an
/// <see cref="Error"/> inside a <see cref="Result"/> over throwing for expected
/// (non-exceptional) control flow.
/// </summary>
/// <param name="Code">A stable, machine-readable identifier, e.g. "User.NotFound".</param>
/// <param name="Message">A human-readable, safe-to-surface description.</param>
/// <param name="Type">The category used for transport mapping.</param>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>Represents the absence of an error.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
}
