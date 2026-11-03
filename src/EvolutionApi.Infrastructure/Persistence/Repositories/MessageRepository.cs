using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.Persistence.Repositories;

public sealed class MessageRepository(AppDbContext db) : Repository<Message>(db), IMessageRepository
{
    private readonly AppDbContext _db = db;

    public async Task<Message?> GetByWaIdAsync(string instanceId, string messageId, CancellationToken ct = default)
    {
        return await _db.Messages
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.InstanceId == instanceId && m.Id == messageId, ct);
    }

    public async Task<IReadOnlyList<Message>> GetChatMessagesAsync(string instanceId, string remoteJid, int limit = 50, CancellationToken ct = default)
    {
        return await _db.Messages
            .AsNoTracking()
            .Where(m => m.InstanceId == instanceId && m.RemoteJid == remoteJid)
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(Message message, CancellationToken ct = default)
    {
        var existing = await _db.Messages
            .SingleOrDefaultAsync(m => m.InstanceId == message.InstanceId && m.Id == message.Id, ct);

        if (existing is null)
        {
            await _db.Messages.AddAsync(message, ct);
        }
        else
        {
            existing.Status = message.Status;
            existing.MessageJson = message.MessageJson;
            existing.PushName = message.PushName ?? existing.PushName;
            existing.Participant = message.Participant ?? existing.Participant;
            existing.Timestamp = message.Timestamp;
        }
    }
}
