using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Shoppy.Business.Caching;

public sealed class CacheService(HybridCache _hybridCache, ILogger<CacheService> _logger) : ICacheService
{
    public async Task<T> GetOrCreateAsync<T>(string prefix, string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        return await _hybridCache.GetOrCreateAsync(
            key,
            async cancellationToken => await factory(),
            new HybridCacheEntryOptions { Expiration = expiration, LocalCacheExpiration = expiration },
            tags: [prefix]);
    }

    public async Task InvalidatePrefixAsync(string prefix)
    {
        // Best-effort: a cache backend outage (e.g. Redis unreachable) must not fail the write
        // that triggered this invalidation, since the underlying DB change already succeeded.
        try
        {
            await _hybridCache.RemoveByTagAsync(prefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache prefix {Prefix}", prefix);
        }
    }
}
