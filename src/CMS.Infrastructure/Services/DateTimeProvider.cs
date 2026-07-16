using CMS.Application.Common.Abstractions;

namespace CMS.Infrastructure.Services;

/// <summary>Default clock returning the real UTC time.</summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
