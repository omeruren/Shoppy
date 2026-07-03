using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Shoppy.Business.Caching;

public sealed class CacheService(IMemoryCache _cache) : ICacheService
{
    // Registered as a DI singleton, so these tokens live on the singleton instance
    // (not static fields on the consuming services) — resettable/swappable per test/DI-scope.
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _prefixTokens = new();

    public async Task<T> GetOrCreateAsync<T>(string prefix, string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory();

        var tokenSource = _prefixTokens.GetOrAdd(prefix, _ => new CancellationTokenSource());

        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(expiration)
            .AddExpirationToken(new CancellationChangeToken(tokenSource.Token));

        _cache.Set(key, value, options);

        return value;
    }

    public void InvalidatePrefix(string prefix)
    {
        if (_prefixTokens.TryRemove(prefix, out var oldTokenSource))
        {
            oldTokenSource.Cancel();
            oldTokenSource.Dispose();
        }
    }
}
