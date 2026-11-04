using EvolutionApi.Domain.Common;
using Finbuckle.MultiTenant.Abstractions;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Message record.
/// Pure POCO entity with zero business logic.
/// Supports multi-tenancy via Finbuckle.
/// </summary>
[MultiTenant]
public sealed class Message : IMultiTenant
{
    public string Id { get; set; } = string.Empty; // WA message ID
    public string? TenantId { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public string RemoteJid { get; set; } = string.Empty;
    public bool FromMe { get; set; }
    public string? Participant { get; set; }
    public string? PushName { get; set; }
    public string MessageJson { get; set; } = string.Empty;
    public string? MessageType { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Instance? Instance { get; set; }
}
