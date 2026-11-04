using System.Text.Json;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.WhatsApp.Services;

/// <summary>
/// Provides find/search operations over the local Chat, Message and Contact tables
/// that are populated by the BaileysNetWhatsAppEngine event handlers.
/// </summary>
public sealed class MessageStoreService(AppDbContext db)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ── Messages ───────────────────────────────────────────────────────────────

    public async Task<JsonElement> FindMessagesAsync(
        string instanceId, string? remoteJid, string? msgId,
        string? status, int limit, CancellationToken ct)
    {
        var q = db.Messages.AsNoTracking()
            .Where(m => m.InstanceId == instanceId);

        if (remoteJid is not null) q = q.Where(m => m.RemoteJid == remoteJid);
        if (msgId is not null) q = q.Where(m => m.Id == msgId);
        if (status is not null) q = q.Where(m => m.Status == status.ToUpperInvariant());

        var rows = await q.OrderByDescending(m => m.Timestamp).Take(limit).ToListAsync(ct);
        var messages = rows.Select(r => JsonSerializer.Deserialize<JsonElement>(r.MessageJson, Json)).ToArray();
        return JsonSerializer.SerializeToElement(messages, Json);
    }

    public async Task<JsonElement> FindStatusMessagesAsync(
        string instanceId, string? status, int limit, CancellationToken ct)
    {
        // Status messages are broadcasts to the special JID
        var q = db.Messages.AsNoTracking()
            .Where(m => m.InstanceId == instanceId && m.RemoteJid == "status@broadcast");
        if (status is not null) q = q.Where(m => m.Status == status.ToUpperInvariant());
        var rows = await q.OrderByDescending(m => m.Timestamp).Take(limit).ToListAsync(ct);
        var messages = rows.Select(r => JsonSerializer.Deserialize<JsonElement>(r.MessageJson, Json)).ToArray();
        return JsonSerializer.SerializeToElement(messages, Json);
    }

    // ── Chats ─────────────────────────────────────────────────────────────────

    public async Task<JsonElement> FindChatsAsync(
        string instanceId, string? jid, string? pushName, int limit, CancellationToken ct)
    {
        var q = db.Chats.AsNoTracking().Where(c => c.InstanceId == instanceId);
        if (jid is not null) q = q.Where(c => c.Id == jid);
        if (pushName is not null) q = q.Where(c => EF.Functions.Like(c.Name, $"%{pushName}%"));

        var rows = await q.OrderByDescending(c => c.UpdatedAt).Take(limit).ToListAsync(ct);
        var result = rows.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            unreadCount = c.UnreadCount,
            archived = c.Archived,
            lastMessage = c.LastMessageJson != null
                ? JsonSerializer.Deserialize<JsonElement>(c.LastMessageJson, Json)
                : (JsonElement?)null
        }).ToArray();
        return JsonSerializer.SerializeToElement(result, Json);
    }

    // ── Contacts ──────────────────────────────────────────────────────────────

    public async Task<JsonElement> FindContactsAsync(
        string instanceId, string? jid, string? pushName, int limit, CancellationToken ct)
    {
        var q = db.Contacts.AsNoTracking().Where(c => c.InstanceId == instanceId);
        if (jid is not null) q = q.Where(c => c.Id == jid);
        if (pushName is not null) q = q.Where(c => EF.Functions.Like(c.PushName, $"%{pushName}%"));

        var rows = await q.OrderBy(c => c.PushName).Take(limit).ToListAsync(ct);
        var result = rows.Select(c => new
        {
            id = c.Id,
            pushName = c.PushName,
            verifiedName = c.VerifiedName,
            profilePicUrl = c.ProfilePicUrl,
            isWhatsApp = c.IsWhatsApp
        }).ToArray();
        return JsonSerializer.SerializeToElement(result, Json);
    }

    // ── Upsert helpers (called from event handlers) ───────────────────────────

    public async Task UpsertMessageAsync(string instanceId, JsonElement msgElement, CancellationToken ct)
    {
        try
        {
            var key = msgElement.GetProperty("key");
            var msgId = key.GetProperty("id").GetString()!;
            var remoteJid = key.GetProperty("remoteJid").GetString()!;
            var fromMe = key.TryGetProperty("fromMe", out var fm) && fm.GetBoolean();
            var participant = key.TryGetProperty("participant", out var p) ? p.GetString() : null;
            var pushName = msgElement.TryGetProperty("pushName", out var pn) ? pn.GetString() : null;
            var msgType = msgElement.TryGetProperty("messageType", out var mt) ? mt.GetString() : null;
            var ts = msgElement.TryGetProperty("messageTimestamp", out var t)
                ? DateTimeOffset.FromUnixTimeSeconds(t.GetInt64()).UtcDateTime
                : DateTime.UtcNow;

            var json = msgElement.GetRawText();
            var existing = await db.Messages
                .SingleOrDefaultAsync(m => m.Id == msgId && m.InstanceId == instanceId, ct);

            if (existing is null)
            {
                db.Messages.Add(new Message
                {
                    Id = msgId, InstanceId = instanceId, RemoteJid = remoteJid,
                    FromMe = fromMe, Participant = participant, PushName = pushName,
                    MessageJson = json, MessageType = msgType,
                    Status = fromMe ? "PENDING" : "RECEIVED", Timestamp = ts
                });
            }
            else
            {
                existing.MessageJson = json;
                existing.PushName = pushName;
                existing.MessageType = msgType;
            }

            // Update chat record
            var chat = await db.Chats
                .SingleOrDefaultAsync(c => c.Id == remoteJid && c.InstanceId == instanceId, ct);
            if (chat is null)
            {
                db.Chats.Add(new Chat
                {
                    Id = remoteJid, InstanceId = instanceId,
                    Name = pushName,
                    LastMessageJson = json, LastMessageAt = ts, UpdatedAt = ts,
                    UnreadCount = fromMe ? 0 : 1
                });
            }
            else
            {
                chat.LastMessageJson = json;
                chat.LastMessageAt = ts;
                chat.UpdatedAt = ts;
                if (!fromMe) chat.UnreadCount++;
                if (pushName is not null && chat.Name is null) chat.Name = pushName;
            }

            await db.SaveChangesAsync(ct);
        }
        catch { /* do not let a persistence failure crash the event loop */ }
    }

    public async Task UpsertContactAsync(string instanceId, string jid, string? pushName, string? verifiedName, CancellationToken ct)
    {
        try
        {
            var existing = await db.Contacts
                .SingleOrDefaultAsync(c => c.Id == jid && c.InstanceId == instanceId, ct);
            if (existing is null)
                db.Contacts.Add(new Contact
                    { Id = jid, InstanceId = instanceId, PushName = pushName, VerifiedName = verifiedName, UpdatedAt = DateTime.UtcNow });
            else
            {
                if (pushName is not null) existing.PushName = pushName;
                if (verifiedName is not null) existing.VerifiedName = verifiedName;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
        }
        catch { }
    }

    public async Task MarkChatReadAsync(string instanceId, string remoteJid, CancellationToken ct)
    {
        try
        {
            var chat = await db.Chats
                .SingleOrDefaultAsync(c => c.Id == remoteJid && c.InstanceId == instanceId, ct);
            if (chat is not null) { chat.UnreadCount = 0; await db.SaveChangesAsync(ct); }
        }
        catch { }
    }

    public async Task ArchiveChatAsync(string instanceId, string remoteJid, bool archive, CancellationToken ct)
    {
        try
        {
            var chat = await db.Chats
                .SingleOrDefaultAsync(c => c.Id == remoteJid && c.InstanceId == instanceId, ct);
            if (chat is not null) { chat.Archived = archive; await db.SaveChangesAsync(ct); }
        }
        catch { }
    }

    public async Task DeleteMessagesAsync(string instanceId, string[] msgIds, string? remoteJid, CancellationToken ct)
    {
        try
        {
            var q = db.Messages.Where(m => m.InstanceId == instanceId && msgIds.Contains(m.Id));
            if (remoteJid is not null) q = q.Where(m => m.RemoteJid == remoteJid);
            await q.ExecuteDeleteAsync(ct);
        }
        catch { }
    }

    public async Task UpdateMessageStatusAsync(string instanceId, string msgId, string status, CancellationToken ct)
    {
        try
        {
            await db.Messages
                .Where(m => m.Id == msgId && m.InstanceId == instanceId)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, status), ct);
        }
        catch { }
    }
}
