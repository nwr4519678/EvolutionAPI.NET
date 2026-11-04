using EvolutionApi.Domain.Entities;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Repository contract for scheduled broadcast message jobs.
/// </summary>
public interface IScheduledMessageRepository : IRepository<ScheduledMessageJob>
{
    Task<IReadOnlyList<ScheduledMessageJob>> GetPendingJobsAsync(DateTime dueBeforeUtc, CancellationToken ct = default);
    Task<ScheduledMessageJob?> GetByHangfireJobIdAsync(string hangfireJobId, CancellationToken ct = default);
}
