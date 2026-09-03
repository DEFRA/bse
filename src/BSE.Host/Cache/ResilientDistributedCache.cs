using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace BSE.Host.Cache;

/// <summary>
/// Wraps a Redis <see cref="IDistributedCache"/> with an in-process <see cref="MemoryDistributedCache"/>
/// fallback. On any Redis connectivity failure the cache silently continues using MemoryCache,
/// logs a warning, and marks <see cref="IsUsingFallback"/> so the
/// <c>/health/ready</c> endpoint can return <c>Degraded</c>.
/// When Redis recovers (next successful call), the flag is cleared and full distributed mode resumes.
/// </summary>
/// <remarks>
/// In fallback mode the application runs as a single-replica only — cache entries are
/// not shared across container instances. This matches the legacy InProc session behaviour
/// and is safe for production use at reduced scale.
/// </remarks>
public sealed class ResilientDistributedCache : IDistributedCache
{
    private readonly IDistributedCache _redis;
    private readonly IDistributedCache _memory;
    private readonly ILogger<ResilientDistributedCache> _logger;

    // volatile so reads in the health check (different thread) always see the latest value.
    private volatile bool _isUsingFallback;

    public bool IsUsingFallback => _isUsingFallback;

    public ResilientDistributedCache(
        IDistributedCache redis,
        IDistributedCache memory,
        ILogger<ResilientDistributedCache> logger)
    {
        _redis = redis;
        _memory = memory;
        _logger = logger;
    }

    // ── Synchronous members ───────────────────────────────────────────────────

    public byte[]? Get(string key)
    {
        try { var v = _redis.Get(key); RecoverIfNeeded(); return v; }
        catch (Exception ex) when (IsCacheFault(ex)) { MarkDegraded(ex); return _memory.Get(key); }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        try { _redis.Set(key, value, options); RecoverIfNeeded(); }
        catch (Exception ex) when (IsCacheFault(ex)) { MarkDegraded(ex); _memory.Set(key, value, options); }
    }

    public void Refresh(string key)
    {
        try { _redis.Refresh(key); RecoverIfNeeded(); }
        catch (Exception ex) when (IsCacheFault(ex)) { MarkDegraded(ex); _memory.Refresh(key); }
    }

    public void Remove(string key)
    {
        try { _redis.Remove(key); RecoverIfNeeded(); }
        catch (Exception ex) when (IsCacheFault(ex)) { MarkDegraded(ex); _memory.Remove(key); }
    }

    // ── Asynchronous members ──────────────────────────────────────────────────

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        try { var v = await _redis.GetAsync(key, token); RecoverIfNeeded(); return v; }
        catch (Exception ex) when (IsCacheFault(ex)) { MarkDegraded(ex); return await _memory.GetAsync(key, token); }
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        try { await _redis.SetAsync(key, value, options, token); RecoverIfNeeded(); }
        catch (Exception ex) when (IsCacheFault(ex)) { MarkDegraded(ex); await _memory.SetAsync(key, value, options, token); }
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        try { await _redis.RefreshAsync(key, token); RecoverIfNeeded(); }
        catch (Exception ex) when (IsCacheFault(ex)) { MarkDegraded(ex); await _memory.RefreshAsync(key, token); }
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try { await _redis.RemoveAsync(key, token); RecoverIfNeeded(); }
        catch (Exception ex) when (IsCacheFault(ex)) { MarkDegraded(ex); await _memory.RemoveAsync(key, token); }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void MarkDegraded(Exception ex)
    {
        if (!_isUsingFallback)
        {
            _logger.LogWarning(
                ex,
                "Redis is unavailable — falling back to in-memory cache. " +
                "The application continues in single-replica degraded mode. " +
                "/health/ready will return Degraded until Redis is restored.");
            _isUsingFallback = true;
        }
    }

    private void RecoverIfNeeded()
    {
        if (_isUsingFallback)
        {
            _logger.LogInformation("Redis connection restored — resuming distributed cache mode.");
            _isUsingFallback = false;
        }
    }

    // Treat all exceptions except cancellation/disposal as transient Redis faults.
    // The only operations performed on IDistributedCache are byte-array get/set, so
    // the only realistic exceptions are connectivity/timeout failures from Redis.
    private static bool IsCacheFault(Exception ex) =>
        ex is not OperationCanceledException and not ObjectDisposedException;
}
