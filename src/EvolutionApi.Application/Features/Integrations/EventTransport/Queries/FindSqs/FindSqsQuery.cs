using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Integrations.EventTransport.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;

namespace EvolutionApi.Application.Features.Integrations.EventTransport.Queries.FindSqs;

public sealed record FindSqsQuery(string InstanceName) : IQuery<FindSqsResult>, ICacheableQuery
{
    public string CacheKey => EventTransportCacheKeys.SqsKey(InstanceName);
    public TimeSpan? Expiration => EventTransportCacheKeys.Expiration;
    public string? CacheTag => EventTransportCacheKeys.SqsTag(InstanceName);
}

public sealed record FindSqsResult(string InstanceName, SqsSettingDto Sqs);

public sealed record SqsSettingDto(
    bool Enabled = false,
    string? AccessKeyId = null,
    string? SecretAccessKey = "****",
    string? AccountId = null,
    string? Region = null);

public sealed class FindSqsQueryHandler(IInstanceRepository instances, IIntegrationRepository integrations)
    : IRequestHandler<FindSqsQuery, FindSqsResult>
{
    public async Task<FindSqsResult> Handle(FindSqsQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");
        var s = await integrations.GetSqsAsync(instance.Id, ct);
        var dto = s is not null
            ? new SqsSettingDto(s.Enabled, s.AccessKeyId, "****", s.AccountId, s.Region)
            : new SqsSettingDto();
        return new FindSqsResult(query.InstanceName, dto);
    }
}
