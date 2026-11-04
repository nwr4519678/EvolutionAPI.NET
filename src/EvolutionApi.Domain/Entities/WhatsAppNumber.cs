using EvolutionApi.Domain.Common;
using EvolutionApi.Domain.Enums;

namespace EvolutionApi.Domain.Entities;

public sealed class WhatsAppNumber : BaseEntity
{
    public required string TenantId { get; set; }
    public required string DisplayName { get; set; }
    public string? PhoneNumber { get; set; }
    public WhatsAppConnectionMethod ConnectionMethod { get; set; } = WhatsAppConnectionMethod.QrPairing;
    public WhatsAppNumberStatus Status { get; set; } = WhatsAppNumberStatus.Pending;
    public string? LinkedInstanceId { get; set; } // links to existing Instance entity
    public string? AiProviderId { get; set; }
    public string? SystemPromptId { get; set; }
    public bool BotEnabled { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? CreatedBy { get; set; }

    // Navigation
    public Tenant? Tenant { get; set; }
    public ICollection<Conversation> Conversations { get; set; } = [];
    public ICollection<UsageRecord> UsageRecords { get; set; } = [];
    public ICollection<BotRule> BotRules { get; set; } = [];
}
