using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Instances.Caching;
using EvolutionApi.Domain.Interfaces;

namespace EvolutionApi.Application.Features.Instances.Queries.GetInstances;

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GetInstancesQuery(string? InstanceName = null)
    : IQuery<IReadOnlyList<InstanceDto>>, ICacheableQuery
{
    public string    CacheKey   => string.IsNullOrEmpty(InstanceName)
                                       ? InstanceCacheKeys.All()
                                       : InstanceCacheKeys.ByName(InstanceName);
    public TimeSpan? Expiration => InstanceCacheKeys.Expiration;
    public string?   CacheTag   => InstanceCacheKeys.Tag;
}

// ── Response DTO ──────────────────────────────────────────────────────────────

public sealed record InstanceDto(
    string   Id,
    string   Name,
    string?  Number,
    string   ConnectionStatus,
    string   Integration,
    DateTime CreatedAt);

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetInstancesQueryHandler(
    IUnitOfWork uow) : MediatR.IRequestHandler<GetInstancesQuery, IReadOnlyList<InstanceDto>>
{
    public async Task<IReadOnlyList<InstanceDto>> Handle(GetInstancesQuery query, CancellationToken ct)
    {
        var instances = await uow.Instances.GetAllWithDetailsAsync(ct);

        return (query.InstanceName is not null
                ? instances.Where(i => i.Name == query.InstanceName)
                : instances)
            .Select(i => new InstanceDto(
                i.Id, i.Name, i.Number, i.ConnectionStatus, i.Integration, i.CreatedAt))
            .ToList();
    }
}
