using Shoppy.Business.Caching;

namespace Shoppy.UnitTests.TestDoubles;

/// <summary>Always calls through to the factory — no caching — so tests always observe fresh DB state.</summary>
internal sealed class NoOpCacheService : ICacheService
{
    public Task<T> GetOrCreateAsync<T>(string prefix, string key, Func<Task<T>> factory, TimeSpan expiration)
        => factory();

    public Task InvalidatePrefixAsync(string prefix) => Task.CompletedTask;
}
