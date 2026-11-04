using EvolutionApi.Domain.Common;
using EvolutionApi.Domain.Enums;

namespace EvolutionApi.Domain.Entities;

/// <summary>Multi-tenant client/company entity.</summary>
public sealed class Tenant : BaseEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; } // URL-safe unique identifier
    public string? LogoUrl { get; set; }
    public string Timezone { get; set; } = "UTC";
    public string Language { get; set; } = "en";
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public string? PlanId { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? CreatedBy { get; set; }

    // Navigation
    public ICollection<WhatsAppNumber> WhatsAppNumbers { get; set; } = [];
    public ICollection<AiProvider> AiProviders { get; set; } = [];
    public ICollection<SystemPrompt> SystemPrompts { get; set; } = [];
    public ICollection<BotRule> BotRules { get; set; } = [];
    public ICollection<UsageRecord> UsageRecords { get; set; } = [];
    public ICollection<PlatformApiKey> ApiKeys { get; set; } = [];
    public ICollection<WebhookEndpoint> WebhookEndpoints { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
