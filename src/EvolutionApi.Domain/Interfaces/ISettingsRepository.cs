using EvolutionApi.Domain.Entities;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Instance settings and proxy configuration repository contract.
/// </summary>
public interface ISettingsRepository
{
    Task<InstanceSettings?> GetSettingsAsync(string instanceId, CancellationToken ct = default);
    Task<InstanceProxy?> GetProxyAsync(string instanceId, CancellationToken ct = default);
    Task AddOrUpdateSettingsAsync(InstanceSettings settings, CancellationToken ct = default);
    Task AddOrUpdateProxyAsync(InstanceProxy proxy, CancellationToken ct = default);
}
