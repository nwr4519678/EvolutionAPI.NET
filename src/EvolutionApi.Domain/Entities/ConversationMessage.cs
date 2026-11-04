using EvolutionApi.Domain.Common;
using EvolutionApi.Domain.Enums;

namespace EvolutionApi.Domain.Entities;

public sealed class ConversationMessage : BaseEntity
{
    public required string TenantId { get; set; }
    public required string ConversationId { get; set; }
    public required string NumberId { get; set; }
    public MessageDirection Direction { get; set; }
    public MessageSender Sender { get; set; } = MessageSender.Contact;
    public string? Body { get; set; }
    public string? MediaType { get; set; }
    public string? MediaUrl { get; set; }
    public string? ProviderMessageId { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public bool IsFromBot { get; set; }
    public string? AiProviderId { get; set; }
    public string? AiModel { get; set; }
    public int? TokensIn { get; set; }
    public int? TokensOut { get; set; }
    public decimal? AiCost { get; set; }
    public int? AiLatencyMs { get; set; }
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Conversation? Conversation { get; set; }
}
