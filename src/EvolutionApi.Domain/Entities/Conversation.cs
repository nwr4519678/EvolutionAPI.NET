using EvolutionApi.Domain.Common;
using EvolutionApi.Domain.Enums;

namespace EvolutionApi.Domain.Entities;

public sealed class Conversation : BaseEntity
{
    public required string TenantId { get; set; }
    public required string NumberId { get; set; }
    public required string ContactPhone { get; set; }
    public string? ContactName { get; set; }
    public ConversationStatus Status { get; set; } = ConversationStatus.Open;
    public bool BotPaused { get; set; }
    public string? AssignedAgentId { get; set; }
    public string? LastMessagePreview { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public int MessageCount { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation
    public WhatsAppNumber? WhatsAppNumber { get; set; }
    public ICollection<ConversationMessage> Messages { get; set; } = [];
}
