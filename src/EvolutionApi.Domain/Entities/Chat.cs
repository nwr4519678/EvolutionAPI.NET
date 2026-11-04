using EvolutionApi.Domain.Common;
using Finbuckle.MultiTenant.Abstractions;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Chat thread record.
/// Pure POCO entity with zero business logic.
/// Supports multi-tenancy via Finbuckle.
/// </summary>
[MultiTenant]
public sealed class Chat : IMultiTenant
{
    public string Id { get; set; } = string.Empty; // Remote JID
    public string? TenantId { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int UnreadCount { get; set; }
    public bool Archived { get; set; }
    public string? LastMessageJson { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Instance? Instance { get; set; }
}
