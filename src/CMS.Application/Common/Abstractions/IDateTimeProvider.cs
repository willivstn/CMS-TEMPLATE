namespace CMS.Application.Common.Abstractions;

/// <summary>
/// Abstraction over the system clock so time-dependent logic is testable.
/// Always exposes UTC; convert to user-local time only at the presentation edge.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
