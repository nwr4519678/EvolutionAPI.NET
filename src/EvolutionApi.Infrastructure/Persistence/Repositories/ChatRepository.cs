using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Chat, message history, and contact repository implementation.
/// </summary>
public sealed class ChatRepository(AppDbContext db) : IChatRepository
{
    public async Task<Chat?> GetChatAsync(string instanceId, string remoteJid, CancellationToken ct = default)
        => await db.Chats.AsNoTracking().FirstOrDefaultAsync(c => c.InstanceId == instanceId && c.Id == remoteJid, ct);

    public async Task<IReadOnlyList<Chat>> GetChatsAsync(string instanceId, CancellationToken ct = default)
        => await db.Chats.AsNoTracking().Where(c => c.InstanceId == instanceId).OrderByDescending(c => c.UpdatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(string instanceId, string? remoteJid, int limit = 50, CancellationToken ct = default)
    {
        var q = db.Messages.AsNoTracking().Where(m => m.InstanceId == instanceId);
        if (!string.IsNullOrWhiteSpace(remoteJid))
            q = q.Where(m => m.RemoteJid == remoteJid);
        return await q.OrderByDescending(m => m.Timestamp).Take(limit).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Contact>> GetContactsAsync(string instanceId, CancellationToken ct = default)
        => await db.Contacts.AsNoTracking().Where(c => c.InstanceId == instanceId).ToListAsync(ct);

    public async Task AddMessageAsync(Message message, CancellationToken ct = default)
        => await db.Messages.AddAsync(message, ct);

    public async Task AddOrUpdateChatAsync(Chat chat, CancellationToken ct = default)
    {
        var existing = await db.Chats.FirstOrDefaultAsync(c => c.InstanceId == chat.InstanceId && c.Id == chat.Id, ct);
        if (existing is null)
            await db.Chats.AddAsync(chat, ct);
        else
        {
            existing.Name = chat.Name ?? existing.Name;
            existing.UnreadCount = chat.UnreadCount;
            existing.Archived = chat.Archived;
            existing.LastMessageJson = chat.LastMessageJson ?? existing.LastMessageJson;
            existing.LastMessageAt = chat.LastMessageAt ?? existing.LastMessageAt;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task AddOrUpdateContactAsync(Contact contact, CancellationToken ct = default)
    {
        var existing = await db.Contacts.FirstOrDefaultAsync(c => c.InstanceId == contact.InstanceId && c.Id == contact.Id, ct);
        if (existing is null)
            await db.Contacts.AddAsync(contact, ct);
        else
        {
            existing.PushName = contact.PushName ?? existing.PushName;
            existing.VerifiedName = contact.VerifiedName ?? existing.VerifiedName;
            existing.ProfilePicUrl = contact.ProfilePicUrl ?? existing.ProfilePicUrl;
            existing.IsWhatsApp = contact.IsWhatsApp ?? existing.IsWhatsApp;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task ArchiveChatAsync(string instanceId, string chatId, bool archive, CancellationToken ct = default)
    {
        var chat = await db.Chats.FirstOrDefaultAsync(c => c.InstanceId == instanceId && c.Id == chatId, ct);
        if (chat is not null)
        {
            chat.Archived = archive;
            chat.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
