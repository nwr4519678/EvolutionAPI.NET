using EvolutionApi.Domain.Common;
using Finbuckle.MultiTenant.Abstractions;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// WhatsApp instance representation.
/// Pure POCO entity with zero business logic.
/// Supports multi-tenancy via ITenantScopedEntity and Finbuckle.
/// </summary>
[MultiTenant]
public sealed class Instance : BaseEntity, ITenantScopedEntity
{
    public string? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public string? Number { get; set; }
    public string Integration { get; set; } = "WHATSAPP-BAILEYS";
    public string ConnectionStatus { get; set; } = "close";
    public string? OwnerJid { get; set; }
    public string? BusinessId { get; set; }
    public string? ProfileName { get; set; }
    public string? ProfilePicUrl { get; set; }

    // Navigation properties
    public WebhookConfig? Webhook { get; set; }
    public InstanceProxy? Proxy { get; set; }
    public InstanceSettings? Settings { get; set; }
    public ICollection<SessionAuth> AuthRecords { get; set; } = new List<SessionAuth>();
    public ICollection<ChatFlowSession> ChatFlowSessions { get; set; } = new List<ChatFlowSession>();
    public ICollection<ScheduledMessageJob> ScheduledMessages { get; set; } = new List<ScheduledMessageJob>();
}
