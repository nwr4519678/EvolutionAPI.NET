using EvolutionApi.Domain.Common;
using Finbuckle.MultiTenant.Abstractions;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Scheduled broadcast or delayed message job handled by Hangfire / background schedulers.
/// </summary>
[MultiTenant]
public sealed class ScheduledMessageJob : BaseEntity, ITenantScopedEntity
{
    public string? TenantId { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Operation { get; set; } = "sendText";
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
    public string Status { get; set; } = "Scheduled"; // Scheduled, Executing, Completed, Failed, Cancelled
    public string? HangfireJobId { get; set; }
    public string? LastError { get; set; }

    // Navigation property
    public Instance? Instance { get; set; }
}
