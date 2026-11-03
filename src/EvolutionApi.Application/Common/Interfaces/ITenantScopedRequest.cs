namespace EvolutionApi.Application.Common.Interfaces;

/// <summary>
/// Marks a MediatR request as tenant-scoped.
/// TenantValidationBehavior uses this to implicitly inject or validate the tenant identity.
/// </summary>
public interface ITenantScopedRequest
{
    string? TenantId { get; set; }
}
