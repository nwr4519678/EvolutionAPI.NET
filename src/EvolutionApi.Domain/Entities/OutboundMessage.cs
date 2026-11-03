using EvolutionApi.Domain.Common;
using Finbuckle.MultiTenant.Abstractions;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Outbound message queue record.
/// Pure POCO entity with zero business logic.
/// Supports multi-tenancy via Finbuckle.
/// </summary>
[MultiTenant]
public sealed class OutboundMessage : BaseEntity, IMultiTenant
{
    public string? TenantId { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeaseUntil { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResultJson { get; set; }
    public string? LastError { get; set; }
}
