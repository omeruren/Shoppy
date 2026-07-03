using Microsoft.Extensions.Caching.Hybrid;

namespace Shoppy.Business.Caching;

public sealed class CacheService(HybridCache _hybridCache) : ICacheService
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
        => await _hybridCache.RemoveByTagAsync(prefix);
}
