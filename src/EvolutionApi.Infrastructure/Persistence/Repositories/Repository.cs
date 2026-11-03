using EvolutionApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EvolutionApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository — implements <see cref="IRepository{T}"/> with no raw SQL.
/// All queries use strongly-typed LINQ, translated to SQL by EF Core.
/// AsNoTracking is used for read operations to avoid unnecessary change-tracking overhead.
/// </summary>
public class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    protected readonly AppDbContext Db  = db;
    protected readonly DbSet<T>    Set  = db.Set<T>();

    // ── Reads ─────────────────────────────────────────────────────────────────

    public async Task<T?> GetByIdAsync(string id, CancellationToken ct = default)
        => await Set.FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await Set.AsNoTracking().ToListAsync(ct);

    public async Task<T?> FindAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await Set.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public async Task<IReadOnlyList<T>> FindAllAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await Set.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await Set.AnyAsync(predicate, ct);

    // ── Writes ────────────────────────────────────────────────────────────────

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await Set.AddAsync(entity, ct);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);
}
