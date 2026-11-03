using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Integrations.EventTransport.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;

namespace EvolutionApi.Application.Features.Integrations.EventTransport.Queries.FindRabbitMq;

public sealed record FindRabbitMqQuery(string InstanceName) : IQuery<FindRabbitMqResult>, ICacheableQuery
{
    public string CacheKey => EventTransportCacheKeys.RabbitMqKey(InstanceName);
    public TimeSpan? Expiration => EventTransportCacheKeys.Expiration;
    public string? CacheTag => EventTransportCacheKeys.RabbitMqTag(InstanceName);
}

public sealed record FindRabbitMqResult(string InstanceName, RabbitMqSettingDto RabbitMq);

public sealed record RabbitMqSettingDto(
    bool Enabled = false,
    string? Uri = null,
    string? Exchange = null,
    bool GlobalEvents = false,
    string EventsJson = "[]");

public sealed class FindRabbitMqQueryHandler(IInstanceRepository instances, IIntegrationRepository integrations)
    : IRequestHandler<FindRabbitMqQuery, FindRabbitMqResult>
{
    public async Task<FindRabbitMqResult> Handle(FindRabbitMqQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");
        var s = await integrations.GetRabbitMqAsync(instance.Id, ct);
        var dto = s is not null
            ? new RabbitMqSettingDto(s.Enabled, s.Uri, s.Exchange, s.GlobalEvents, s.EventsJson)
            : new RabbitMqSettingDto();
        return new FindRabbitMqResult(query.InstanceName, dto);
    }
}
