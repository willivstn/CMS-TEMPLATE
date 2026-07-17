using System.Collections.Concurrent;
using CMS.Application.Common.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace CMS.Infrastructure.Caching;

/// <summary>
/// In-memory <see cref="ICacheService"/>. Tracks live keys so entries can be
/// invalidated by prefix (which <see cref="IMemoryCache"/> does not support
/// natively). Registered as a singleton alongside the singleton memory cache.
/// Swap for a Redis-backed implementation to go distributed.
/// </summary>
public sealed class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public async Task<T> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        cache.Set(key, value, ttl ?? DefaultTtl);
        _keys.TryAdd(key, 0);
        return value;
    }

    public void Remove(string key)
    {
        cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        foreach (var key in _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
        {
            cache.Remove(key);
            _keys.TryRemove(key, out _);
        }
    }
}
