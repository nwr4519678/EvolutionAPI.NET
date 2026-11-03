using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Integrations.EventTransport.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;

namespace EvolutionApi.Application.Features.Integrations.EventTransport.Queries.FindNats;

public sealed record FindNatsQuery(string InstanceName) : IQuery<FindNatsResult>, ICacheableQuery
{
    public string CacheKey => EventTransportCacheKeys.NatsKey(InstanceName);
    public TimeSpan? Expiration => EventTransportCacheKeys.Expiration;
    public string? CacheTag => EventTransportCacheKeys.NatsTag(InstanceName);
}

public sealed record FindNatsResult(string InstanceName, NatsSettingDto Nats);

public sealed record NatsSettingDto(
    bool Enabled = false,
    string? Uri = null,
    string EventsJson = "[]");

public sealed class FindNatsQueryHandler(IInstanceRepository instances, IIntegrationRepository integrations)
    : IRequestHandler<FindNatsQuery, FindNatsResult>
{
    public async Task<FindNatsResult> Handle(FindNatsQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");
        var s = await integrations.GetNatsAsync(instance.Id, ct);
        var dto = s is not null
            ? new NatsSettingDto(s.Enabled, s.Uri, s.EventsJson)
            : new NatsSettingDto();
        return new FindNatsResult(query.InstanceName, dto);
    }
}
