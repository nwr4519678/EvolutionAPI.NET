using EvolutionApi.Domain.Entities;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Repository contract for WhatsApp message persistence and history.
/// </summary>
public interface IMessageRepository : IRepository<Message>
{
    Task<Message?> GetByWaIdAsync(string instanceId, string messageId, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetChatMessagesAsync(string instanceId, string remoteJid, int limit = 50, CancellationToken ct = default);
    Task UpsertAsync(Message message, CancellationToken ct = default);
}
