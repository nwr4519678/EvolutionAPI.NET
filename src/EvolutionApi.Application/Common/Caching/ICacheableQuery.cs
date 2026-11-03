namespace EvolutionApi.Application.Common.Caching;

/// <summary>
/// Marker interface for MediatR queries that should be cached via HybridCache.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>Unique cache key for this query request.</summary>
    string CacheKey { get; }

    /// <summary>Optional custom cache expiration. Defaults to 5 minutes if null.</summary>
    TimeSpan? Expiration => null;

    /// <summary>Optional cache tag for bulk invalidation (e.g. "instance:{instanceName}").</summary>
    string? CacheTag => null;
}
