using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Labels.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Labels.Queries.FindLabels;

public sealed record FindLabelsQuery(string InstanceName) : IQuery<JsonElement>, ICacheableQuery
{
    public string CacheKey => LabelCacheKeys.All(InstanceName);
    public TimeSpan? Expiration => LabelCacheKeys.Expiration;
    public string? CacheTag => LabelCacheKeys.Tag(InstanceName);
}

public sealed class FindLabelsQueryHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine) : IRequestHandler<FindLabelsQuery, JsonElement>
{
    public async Task<JsonElement> Handle(FindLabelsQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");
        return await engine.FetchLabelsAsync(instance.Id, ct);
    }
}
