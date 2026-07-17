namespace CMS.Application.Common.Abstractions;

/// <summary>
/// Application cache abstraction. Backed by in-memory caching by default and
/// distributed-cache-ready (swap the implementation for Redis without touching
/// callers). Supports prefix-based invalidation for grouped keys.
/// </summary>
public interface ICacheService
{
    /// <summary>Returns the cached value or produces, caches and returns it.</summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>Removes a single cache entry.</summary>
    void Remove(string key);

    /// <summary>Removes every entry whose key starts with <paramref name="prefix"/>.</summary>
    void RemoveByPrefix(string prefix);
}
