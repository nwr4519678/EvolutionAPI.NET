using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Instance settings and proxy configuration repository implementation.
/// </summary>
public sealed class SettingsRepository(AppDbContext db) : ISettingsRepository
{
    public async Task<InstanceSettings?> GetSettingsAsync(string instanceId, CancellationToken ct = default)
        => await db.InstanceSettings.AsNoTracking().FirstOrDefaultAsync(s => s.InstanceId == instanceId, ct);

    public async Task<InstanceProxy?> GetProxyAsync(string instanceId, CancellationToken ct = default)
        => await db.InstanceProxies.AsNoTracking().FirstOrDefaultAsync(p => p.InstanceId == instanceId, ct);

    public async Task AddOrUpdateSettingsAsync(InstanceSettings settings, CancellationToken ct = default)
    {
        var existing = await db.InstanceSettings.FirstOrDefaultAsync(s => s.InstanceId == settings.InstanceId, ct);
        if (existing is null)
            await db.InstanceSettings.AddAsync(settings, ct);
        else
        {
            existing.RejectCall = settings.RejectCall;
            existing.MsgCall = settings.MsgCall;
            existing.GroupsIgnore = settings.GroupsIgnore;
            existing.AlwaysOnline = settings.AlwaysOnline;
            existing.ReadMessages = settings.ReadMessages;
            existing.ReadStatus = settings.ReadStatus;
            existing.SyncFullHistory = settings.SyncFullHistory;
            existing.WavoipToken = settings.WavoipToken;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task AddOrUpdateProxyAsync(InstanceProxy proxy, CancellationToken ct = default)
    {
        var existing = await db.InstanceProxies.FirstOrDefaultAsync(p => p.InstanceId == proxy.InstanceId, ct);
        if (existing is null)
            await db.InstanceProxies.AddAsync(proxy, ct);
        else
        {
            existing.Enabled = proxy.Enabled;
            existing.Host = proxy.Host;
            existing.Port = proxy.Port;
            existing.Protocol = proxy.Protocol;
            existing.Username = proxy.Username;
            existing.PasswordEncrypted = proxy.PasswordEncrypted;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }
}
