using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Proxy.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;

namespace EvolutionApi.Application.Features.Proxy.Queries.FindProxy;

public sealed record FindProxyQuery(string InstanceName) : IQuery<FindProxyResult>,
 ICacheableQuery
{
    public string CacheKey => ProxyCacheKeys.Key(InstanceName);
    public TimeSpan? Expiration => ProxyCacheKeys.Expiration;
    public string? CacheTag => ProxyCacheKeys.Tag(InstanceName);
}

public sealed record FindProxyResult(
    string InstanceName,
    string Host,
    string Port,
    string Protocol,
    string? Username);

public sealed class FindProxyQueryHandler(
    IInstanceRepository instances,
    ISettingsRepository settings) : IRequestHandler<FindProxyQuery, FindProxyResult>
{
    public async Task<FindProxyResult> Handle(FindProxyQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");

        var proxy = await settings.GetProxyAsync(instance.Id, ct);

        return new FindProxyResult(
            query.InstanceName,
            proxy?.Host     ?? string.Empty,
            proxy?.Port     ?? string.Empty,
            proxy?.Protocol ?? string.Empty,
            proxy?.Username);
    }
}
