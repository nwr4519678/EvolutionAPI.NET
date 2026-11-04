using EvolutionApi.Domain.Entities;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Chat, message history, and contact repository contract.
/// </summary>
public interface IChatRepository
{
    Task<Chat?> GetChatAsync(string instanceId, string remoteJid, CancellationToken ct = default);
    Task<IReadOnlyList<Chat>> GetChatsAsync(string instanceId, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetMessagesAsync(string instanceId, string? remoteJid, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<Contact>> GetContactsAsync(string instanceId, CancellationToken ct = default);
    Task AddMessageAsync(Message message, CancellationToken ct = default);
    Task AddOrUpdateChatAsync(Chat chat, CancellationToken ct = default);
    Task AddOrUpdateContactAsync(Contact contact, CancellationToken ct = default);
    Task ArchiveChatAsync(string instanceId, string chatId, bool archive, CancellationToken ct = default);
}
