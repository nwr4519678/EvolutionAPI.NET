using EvolutionApi.Domain.Common;
using Finbuckle.MultiTenant.Abstractions;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// ChatFlow execution session state per chat/instance.
/// </summary>
[MultiTenant]
public sealed class ChatFlowSession : BaseEntity, ITenantScopedEntity
{
    public string? TenantId { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public string RemoteJid { get; set; } = string.Empty;
    public string CurrentNodeId { get; set; } = string.Empty;
    public string VariablesJson { get; set; } = "{}";
    public string Status { get; set; } = "Active";
    public DateTime LastInteractionAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Instance? Instance { get; set; }
}
