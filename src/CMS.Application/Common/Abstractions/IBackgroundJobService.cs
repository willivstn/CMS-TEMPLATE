using System.Linq.Expressions;

namespace CMS.Application.Common.Abstractions;

/// <summary>
/// Abstraction over the background-job engine (Hangfire) so Application code can
/// enqueue and schedule work without a hard dependency on the implementation.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>Enqueues fire-and-forget work to run as soon as a worker is free.</summary>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>Schedules work to run once after <paramref name="delay"/>.</summary>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);
}
