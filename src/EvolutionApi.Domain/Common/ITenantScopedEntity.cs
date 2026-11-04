namespace EvolutionApi.Domain.Common;

/// <summary>
/// Contract for any domain entity belonging to a specific tenant.
/// Used by Finbuckle and EF Core multi-tenancy query filters.
/// </summary>
public interface ITenantScopedEntity
{
    string? TenantId { get; set; }
}

/// <summary>
/// Multi-tenant marker interface for domain entities.
/// </summary>
public interface IMultiTenant : ITenantScopedEntity
{
}
