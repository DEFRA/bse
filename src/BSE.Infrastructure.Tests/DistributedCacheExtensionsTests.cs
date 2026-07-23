using BSE.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BSE.Infrastructure.Tests;

public sealed class DistributedCacheExtensionsTests
{
    private sealed record TestPayload(string Name, int Value);

    private static IDistributedCache CreateMemoryCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task SetJsonAsync_ThenGetJsonAsync_RoundTrips()
    {
        var cache = CreateMemoryCache();
        var payload = new TestPayload("case-123", 42);

        await cache.SetJsonAsync("test-key", payload);
        var result = await cache.GetJsonAsync<TestPayload>("test-key");

        result.Should().NotBeNull();
        result!.Name.Should().Be("case-123");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetJsonAsync_OnCacheMiss_ReturnsDefault()
    {
        var cache = CreateMemoryCache();

        var result = await cache.GetJsonAsync<TestPayload>("nonexistent-key");

        result.Should().BeNull();
    }

    [Fact]
    public void DefaultWizardOptions_HasSixtyMinuteSlidingExpiry()
    {
        var options = BSE.Infrastructure.Cache.DistributedCacheExtensions.DefaultWizardOptions;
        options.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(60));
    }

    [Fact]
    public async Task SetJsonAsync_CustomOptions_UsesProvidedOptions()
    {
        var cache = CreateMemoryCache();
        var customOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(1)
        };

        await cache.SetJsonAsync("expiring-key", new TestPayload("x", 1), customOptions);
        await Task.Delay(50); // let the entry expire

        var result = await cache.GetJsonAsync<TestPayload>("expiring-key");
        result.Should().BeNull();
    }
}
