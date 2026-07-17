using System.Linq.Expressions;
using CMS.Application.Common.Abstractions;
using Hangfire;

namespace CMS.Web.Jobs;

/// <summary>Thin adapter over Hangfire's client implementing <see cref="IBackgroundJobService"/>.</summary>
public sealed class BackgroundJobService(IBackgroundJobClient client) : IBackgroundJobService
{
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) => client.Enqueue(methodCall);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) => client.Schedule(methodCall, delay);
}
