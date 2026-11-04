using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Webhooks.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;

namespace EvolutionApi.Application.Features.Webhooks.Queries.FindWebhook;

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record FindWebhookQuery(string InstanceName) : IQuery<FindWebhookResult>, ICacheableQuery
{
    public string CacheKey => WebhookCacheKeys.Key(InstanceName);
    public TimeSpan? Expiration => WebhookCacheKeys.Expiration;
    public string? CacheTag => WebhookCacheKeys.Tag(InstanceName);
}

public sealed record FindWebhookResult(
    string InstanceName,
    bool Enabled,
    string? Url,
    string EventsJson,
    bool ByEvents,
    bool Base64);

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class FindWebhookQueryHandler(
    IInstanceRepository instances,
    IWebhookRepository webhooks) : IRequestHandler<FindWebhookQuery, FindWebhookResult>
{
    public async Task<FindWebhookResult> Handle(FindWebhookQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");

        var wh = await webhooks.GetByInstanceIdAsync(instance.Id, ct);

        return new FindWebhookResult(
            query.InstanceName,
            wh?.Enabled    ?? false,
            wh?.Url,
            wh?.EventsJson ?? "[]",
            wh?.ByEvents   ?? false,
            wh?.Base64     ?? false);
    }
}
