using System.Linq.Expressions;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Generic repository interface — no EF Core, SQL, or Infrastructure concerns leak into the Domain.
/// Implementations live in the Infrastructure layer.
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>Returns the entity with the given primary key, or null if not found.</summary>
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Returns all entities. Prefer specific repository methods for filtered reads.</summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the first entity matching the predicate, or null.
    /// Use for simple, infrequent lookups — avoid in hot paths (no index hints).
    /// </summary>
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Returns all entities matching the predicate.</summary>
    Task<IReadOnlyList<T>> FindAllAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Stages the entity for insertion. Caller must call UoW.SaveChangesAsync().</summary>
    Task AddAsync(T entity, CancellationToken ct = default);

    /// <summary>Marks the entity as modified. Caller must call UoW.SaveChangesAsync().</summary>
    void Update(T entity);

    /// <summary>Marks the entity for deletion. Caller must call UoW.SaveChangesAsync().</summary>
    void Remove(T entity);

    /// <summary>Returns true if any entity matches the predicate.</summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}
