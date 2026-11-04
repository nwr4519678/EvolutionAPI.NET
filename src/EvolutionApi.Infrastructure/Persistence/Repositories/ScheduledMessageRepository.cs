using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.Persistence.Repositories;

public sealed class ScheduledMessageRepository(AppDbContext db) : Repository<ScheduledMessageJob>(db), IScheduledMessageRepository
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<ScheduledMessageJob>> GetPendingJobsAsync(DateTime dueBeforeUtc, CancellationToken ct = default)
    {
        return await _db.ScheduledMessages
            .Where(j => j.Status == "Scheduled" && j.ScheduledAtUtc <= dueBeforeUtc)
            .OrderBy(j => j.ScheduledAtUtc)
            .ToListAsync(ct);
    }

    public async Task<ScheduledMessageJob?> GetByHangfireJobIdAsync(string hangfireJobId, CancellationToken ct = default)
    {
        return await _db.ScheduledMessages
            .SingleOrDefaultAsync(j => j.HangfireJobId == hangfireJobId, ct);
    }
}
