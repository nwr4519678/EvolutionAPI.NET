using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Settings.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;

namespace EvolutionApi.Application.Features.Settings.Queries.GetSettings;

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GetSettingsQuery(string InstanceName) : IQuery<GetSettingsResult>, ICacheableQuery
{
    public string CacheKey => SettingsCacheKeys.Key(InstanceName);
    public TimeSpan? Expiration => SettingsCacheKeys.Expiration;
    public string? CacheTag => SettingsCacheKeys.Tag(InstanceName);
}

public sealed record GetSettingsResult(
    string InstanceName,
    bool RejectCall,
    string MsgCall,
    bool GroupsIgnore,
    bool AlwaysOnline,
    bool ReadMessages,
    bool ReadStatus,
    bool SyncFullHistory,
    string WavoipToken);

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetSettingsQueryHandler(
    IInstanceRepository instances,
    ISettingsRepository settings) : IRequestHandler<GetSettingsQuery, GetSettingsResult>
{
    public async Task<GetSettingsResult> Handle(GetSettingsQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");

        var s = await settings.GetSettingsAsync(instance.Id, ct);

        return new GetSettingsResult(
            query.InstanceName,
            s?.RejectCall      ?? false,
            s?.MsgCall         ?? string.Empty,
            s?.GroupsIgnore    ?? false,
            s?.AlwaysOnline    ?? false,
            s?.ReadMessages    ?? false,
            s?.ReadStatus      ?? false,
            s?.SyncFullHistory ?? false,
            s?.WavoipToken     ?? string.Empty);
    }
}
