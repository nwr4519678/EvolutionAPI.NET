using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Groups.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Groups.Queries.GetAllGroups;

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GetAllGroupsQuery(string InstanceName) : IQuery<JsonElement>, ICacheableQuery
{
    public string CacheKey => GroupCacheKeys.All(InstanceName);
    public TimeSpan? Expiration => GroupCacheKeys.Expiration;
    public string? CacheTag => GroupCacheKeys.Tag(InstanceName);
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetAllGroupsQueryHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine) : IRequestHandler<GetAllGroupsQuery, JsonElement>
{
    public async Task<JsonElement> Handle(GetAllGroupsQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");

        return await engine.FetchAllGroupsAsync(instance.Id, ct);
    }
}
