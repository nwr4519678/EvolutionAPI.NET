using System.Collections.Concurrent;
using System.Text.Json;
using Baileys.Types;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.WhatsApp.Services;

/// <summary>
/// SQL Server-backed <see cref="ISignalKeyStore"/> that persists every Signal key category
/// (pre-keys, sessions, sender-keys, app-state-sync, identity-keys, device-lists, lid-mappings,
/// tc-tokens, sender-key-memories) in the existing <c>SessionAuth</c> table.
/// Keys are stored as <c>signal:{type}:{id}</c> entries, encrypted via <see cref="ISecretProtector"/>.
/// </summary>
public sealed class SqlSignalKeyStore(string instanceId, IServiceScopeFactory scopes, ISecretProtector protector)
    : ISignalKeyStore
{
    // Per-(type,id) semaphores to serialise concurrent writes to the same key.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> WriteGates = new(StringComparer.Ordinal);

    private static string Row(string type, string id) => $"signal:{type}:{id}";

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, byte[]?>> GetAsync(
        string type,
        IReadOnlyList<string> ids,
        CancellationToken cancellationToken)
    {
        var keys = ids.Select(id => Row(type, id)).ToList();
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rows = await db.SessionAuths
            .AsNoTracking()
            .Where(x => x.InstanceId == instanceId && keys.Contains(x.SessionKey))
            .Select(x => new { x.SessionKey, x.EncryptedJson })
            .ToListAsync(cancellationToken);

        var lookup = rows.ToDictionary(r => r.SessionKey, r => r.EncryptedJson);
        var result = new Dictionary<string, byte[]?>(ids.Count, StringComparer.Ordinal);
        foreach (var id in ids)
        {
            var rowKey = Row(type, id);
            if (lookup.TryGetValue(rowKey, out var encrypted))
            {
                var json = protector.Unprotect(encrypted);
                result[id] = json is null ? null : Convert.FromBase64String(json);
            }
            else
            {
                result[id] = null;
            }
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string type,
        IReadOnlyDictionary<string, byte[]?> values,
        CancellationToken cancellationToken)
    {
        if (values.Count == 0) return;

        // Serialise each (type,id) individually so concurrent writes don't race.
        foreach (var (id, bytes) in values)
        {
            var gate = Gate(type, id);
            await gate.WaitAsync(cancellationToken);
            try
            {
                var rowKey = Row(type, id);
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (bytes is null)
                {
                    // null entry means delete the key
                    await db.SessionAuths
                        .Where(x => x.InstanceId == instanceId && x.SessionKey == rowKey)
                        .ExecuteDeleteAsync(cancellationToken);
                    continue;
                }

                var plainJson = Convert.ToBase64String(bytes);
                var encrypted = protector.Protect(plainJson);

                var row = await db.SessionAuths
                    .SingleOrDefaultAsync(x => x.InstanceId == instanceId && x.SessionKey == rowKey, cancellationToken);
                if (row is null)
                    db.SessionAuths.Add(new SessionAuth
                        { InstanceId = instanceId, SessionKey = rowKey, EncryptedJson = encrypted });
                else
                {
                    row.EncryptedJson = encrypted;
                    row.UpdatedAt = DateTime.UtcNow;
                }
                await db.SaveChangesAsync(cancellationToken);
            }
            finally { gate.Release(); }
        }
    }

    /// <inheritdoc/>
    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.SessionAuths
            .Where(x => x.InstanceId == instanceId && x.SessionKey.StartsWith("signal:"))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static SemaphoreSlim Gate(string type, string id) =>
        WriteGates.GetOrAdd(type + "\0" + id, _ => new SemaphoreSlim(1, 1));

    // ── Generic key access (used by WaMessageEncryptor) ───────────────────────

    public async Task<string?> GetKeyAsync(string sessionKey, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.SessionAuths.AsNoTracking()
            .Where(x => x.InstanceId == instanceId && x.SessionKey == sessionKey)
            .Select(x => x.EncryptedJson).SingleOrDefaultAsync(ct);
        return row is null ? null : protector.Unprotect(row);
    }

    public async Task SetKeyAsync(string sessionKey, string value, CancellationToken ct)
    {
        var encrypted = protector.Protect(value);
        var gate = WriteGates.GetOrAdd(sessionKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existing = await db.SessionAuths
                .SingleOrDefaultAsync(x => x.InstanceId == instanceId && x.SessionKey == sessionKey, ct);
            if (existing is null)
                db.SessionAuths.Add(new SessionAuth
                    { InstanceId = instanceId, SessionKey = sessionKey, EncryptedJson = encrypted });
            else
                existing.EncryptedJson = encrypted;
            await db.SaveChangesAsync(ct);
        }
        finally { gate.Release(); }
    }

    // ── Generic Signal key storage (used by encryptor/decryptor) ──────────────

    /// <summary>Reads a typed Signal key record by category and key identifier.</summary>
    public async Task<string?> GetKeyAsync(string category, int keyId, CancellationToken ct)
        => await GetKeyAsync(category, keyId.ToString(), ct);

    /// <summary>Reads a typed Signal key record by category and string key identifier.</summary>
    public async Task<string?> GetKeyAsync(string category, string key, CancellationToken ct)
    {
        var sessionKey = $"signal:{category}:{key}";
        return await GetKeyAsync(sessionKey, ct);
    }

    /// <summary>Writes a typed Signal key record.</summary>
    public async Task SetKeyAsync(string category, string key, string value, CancellationToken ct)
    {
        var sessionKey = $"signal:{category}:{key}";
        await SetKeyAsync(sessionKey, value, ct);
    }

    /// <summary>Deletes a typed Signal key record (e.g. consumed one-time pre-key).</summary>
    public async Task DeleteKeyAsync(string category, int keyId, CancellationToken ct)
    {
        var sessionKey = $"signal:{category}:{keyId}";
        var gate = WriteGates.GetOrAdd(sessionKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rows = db.SessionAuths
                .Where(x => x.InstanceId == instanceId && x.SessionKey == sessionKey);
            db.SessionAuths.RemoveRange(rows);
            await db.SaveChangesAsync(ct);
        }
        finally { gate.Release(); }
    }
}
