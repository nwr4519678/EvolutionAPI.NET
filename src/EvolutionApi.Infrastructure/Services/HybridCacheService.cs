using System.Collections.Concurrent;
using EvolutionApi.Domain.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace EvolutionApi.Infrastructure.Services;

/// <summary>
/// HybridCache-backed implementation of ICacheService.
/// Combines L1 (in-process memory) with L2 (distributed) cache, stampede protection, and tag invalidation.
/// </summary>
public sealed class HybridCacheService(HybridCache cache) : ICacheService
{
    private static readonly ConcurrentDictionary<string, byte> TrackedKeys = new();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync<T?>(
            key,
            _ => ValueTask.FromResult<T?>(default),
            new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableUnderlyingData },
            cancellationToken: ct);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        TrackedKeys.TryAdd(key, 0);
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiry ?? TimeSpan.FromMinutes(5),
            LocalCacheExpiration = expiry ?? TimeSpan.FromMinutes(5)
        };
        await cache.SetAsync(key, value, options, cancellationToken: ct);
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiry = null,
        IEnumerable<string>? tags = null,
        CancellationToken ct = default)
    {
        TrackedKeys.TryAdd(key, 0);
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiry ?? TimeSpan.FromMinutes(5),
            LocalCacheExpiration = expiry ?? TimeSpan.FromMinutes(5)
        };

        return await cache.GetOrCreateAsync(
            key,
            factory,
            options,
            tags,
            cancellationToken: ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        TrackedKeys.TryRemove(key, out _);
        await cache.RemoveAsync(key, ct);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var matching = TrackedKeys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in matching)
        {
            TrackedKeys.TryRemove(key, out _);
            await cache.RemoveAsync(key, ct);
        }
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken ct = default)
    {
        await cache.RemoveByTagAsync(tag, ct);
    }
}
