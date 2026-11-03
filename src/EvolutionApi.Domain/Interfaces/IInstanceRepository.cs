using EvolutionApi.Domain.Entities;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Instance repository contract for instance-specific queries.
/// </summary>
public interface IInstanceRepository : IRepository<Instance>
{
    Task<Instance?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Instance?> GetByApiKeyHashAsync(string hash, CancellationToken ct = default);
    Task<IReadOnlyList<Instance>> GetAllWithDetailsAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string name, CancellationToken ct = default);
}
