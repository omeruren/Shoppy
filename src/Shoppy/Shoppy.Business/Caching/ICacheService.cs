namespace Shoppy.Business.Caching;

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string prefix, string key, Func<Task<T>> factory, TimeSpan expiration);

    Task InvalidatePrefixAsync(string prefix);
}
