using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that executes hybrid caching for any ICacheableQuery.
/// Utilizes L1 in-memory + L2 distributed caching with stampede protection and tag invalidation.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(
    ICacheService cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheable)
        {
            return await next();
        }

        var tags = cacheable.CacheTag is not null ? new[] { cacheable.CacheTag } : null;

        logger.LogDebug("[HybridCache] Evaluating cache key '{Key}' with tags: {Tags}",
            cacheable.CacheKey, tags != null ? string.Join(", ", tags) : "none");

        return await cache.GetOrCreateAsync(
            cacheable.CacheKey,
            async ct =>
            {
                logger.LogDebug("[HybridCache] Cache miss for key '{Key}'. Invoking query handler...", cacheable.CacheKey);
                return await next();
            },
            cacheable.Expiration,
            tags,
            cancellationToken);
    }
}
