using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Webhook configuration repository implementation.
/// </summary>
public sealed class WebhookRepository(AppDbContext db) : Repository<WebhookConfig>(db), IWebhookRepository
{
    public async Task<WebhookConfig?> GetByInstanceIdAsync(string instanceId, CancellationToken ct = default)
        => await Set.AsNoTracking()
            .FirstOrDefaultAsync(w => w.InstanceId == instanceId, ct);
}
