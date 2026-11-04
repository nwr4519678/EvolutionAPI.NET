using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Instance repository implementation.
/// </summary>
public sealed class InstanceRepository(AppDbContext db) : Repository<Instance>(db), IInstanceRepository
{
    public async Task<Instance?> GetByNameAsync(string name, CancellationToken ct = default)
        => await Set.AsNoTracking()
            .Include(i => i.Webhook)
            .Include(i => i.Settings)
            .FirstOrDefaultAsync(i => i.Name == name, ct);

    public async Task<Instance?> GetByApiKeyHashAsync(string hash, CancellationToken ct = default)
        => await Set.AsNoTracking()
            .FirstOrDefaultAsync(i => i.ApiKeyHash == hash, ct);

    public async Task<IReadOnlyList<Instance>> GetAllWithDetailsAsync(CancellationToken ct = default)
        => await Set.AsNoTracking()
            .Include(i => i.Webhook)
            .Include(i => i.Settings)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(string name, CancellationToken ct = default)
        => await Set.AnyAsync(i => i.Name == name, ct);
}
