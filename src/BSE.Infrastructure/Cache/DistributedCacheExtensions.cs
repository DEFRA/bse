using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace BSE.Infrastructure.Cache;

public static class DistributedCacheExtensions
{
    /// <summary>
    /// Default sliding expiry for BSE wizard session state.
    /// Equivalent to the legacy 20-minute InProc session plus an additional buffer,
    /// giving users 60 minutes of inactivity before their wizard state expires.
    /// Configurable via <c>BseCache__WizardStateExpiryMinutes</c> if needed.
    /// </summary>
    public static DistributedCacheEntryOptions DefaultWizardOptions =>
        new() { SlidingExpiration = TimeSpan.FromMinutes(60) };

    /// <summary>Deserialises a cached JSON value, or returns <c>default</c> on a cache miss.</summary>
    public static async Task<T?> GetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
    }

    /// <summary>Serialises <paramref name="value"/> as JSON and writes it to the cache.</summary>
    public static Task SetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        return cache.SetAsync(key, bytes, options ?? DefaultWizardOptions, cancellationToken);
    }
}
