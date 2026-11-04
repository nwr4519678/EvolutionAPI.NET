using EvolutionApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EvolutionApi.Infrastructure.WhatsApp.Services;

/// <summary>Persists one Baileys-style auth key record per key name; requires a stable 32-byte base64 encryption key.</summary>
public sealed class SessionKeyStore(IServiceScopeFactory scopes, ISecretProtector protector)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> WriteGates = new(StringComparer.Ordinal);

    public void Save(string instanceId, string sessionKey, string json)
    {
        var gate = Gate(instanceId, sessionKey);
        gate.Wait();
        try
        {
            var payload = protector.Protect(json);
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = db.SessionAuths.SingleOrDefault(x => x.InstanceId == instanceId && x.SessionKey == sessionKey);
            if (row is null) db.SessionAuths.Add(new SessionAuth { InstanceId = instanceId, SessionKey = sessionKey, EncryptedJson = payload });
            else { row.EncryptedJson = payload; row.UpdatedAt = DateTime.UtcNow; }
            db.SaveChanges();
        }
        finally { gate.Release(); }
    }

    public string? Read(string instanceId, string sessionKey)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encrypted = db.SessionAuths.AsNoTracking()
            .Where(x => x.InstanceId == instanceId && x.SessionKey == sessionKey)
            .Select(x => x.EncryptedJson)
            .SingleOrDefault();
        return encrypted is null ? null : protector.Unprotect(encrypted);
    }

    public async Task SaveAsync(string instanceId, string sessionKey, string json, CancellationToken ct)
    {
        var gate = Gate(instanceId, sessionKey);
        await gate.WaitAsync(ct);
        try
        {
            var payload = protector.Protect(json);
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.SessionAuths.SingleOrDefaultAsync(x => x.InstanceId == instanceId && x.SessionKey == sessionKey, ct);
            if (row is null) db.SessionAuths.Add(new SessionAuth { InstanceId = instanceId, SessionKey = sessionKey, EncryptedJson = payload });
            else { row.EncryptedJson = payload; row.UpdatedAt = DateTime.UtcNow; }
            await db.SaveChangesAsync(ct);
        }
        finally { gate.Release(); }
    }

    public async Task<string?> ReadAsync(string instanceId, string sessionKey, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encrypted = await db.SessionAuths.AsNoTracking()
            .Where(x => x.InstanceId == instanceId && x.SessionKey == sessionKey)
            .Select(x => x.EncryptedJson)
            .SingleOrDefaultAsync(ct);
        return encrypted is null ? null : protector.Unprotect(encrypted);
    }

    public async Task DeleteAsync(string instanceId, string sessionKey, CancellationToken ct)
    {
        var gate = Gate(instanceId, sessionKey);
        await gate.WaitAsync(ct);
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.SessionAuths.Where(x => x.InstanceId == instanceId && x.SessionKey == sessionKey).ExecuteDeleteAsync(ct);
        }
        finally { gate.Release(); }
    }

    public async Task DeleteAllAsync(string instanceId, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.SessionAuths.Where(x => x.InstanceId == instanceId).ExecuteDeleteAsync(ct);
    }

    private static SemaphoreSlim Gate(string instanceId, string sessionKey) =>
        WriteGates.GetOrAdd(instanceId + "\0" + sessionKey, _ => new SemaphoreSlim(1, 1));
}
