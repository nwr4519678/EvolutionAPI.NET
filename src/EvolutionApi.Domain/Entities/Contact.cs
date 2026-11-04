using EvolutionApi.Domain.Common;
using Finbuckle.MultiTenant.Abstractions;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Contact record.
/// Pure POCO entity with zero business logic.
/// Supports multi-tenancy via Finbuckle.
/// </summary>
[MultiTenant]
public sealed class Contact : IMultiTenant
{
    public string Id { get; set; } = string.Empty; // Contact JID
    public string? TenantId { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public string? PushName { get; set; }
    public string? VerifiedName { get; set; }
    public string? ProfilePicUrl { get; set; }
    public bool? IsWhatsApp { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Instance? Instance { get; set; }
}
