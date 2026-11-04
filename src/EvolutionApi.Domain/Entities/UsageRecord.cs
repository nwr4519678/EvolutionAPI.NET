using EvolutionApi.Domain.Common;
using EvolutionApi.Domain.Enums;

namespace EvolutionApi.Domain.Entities;

public sealed class UsageRecord : BaseEntity
{
    public required string TenantId { get; set; }
    public required string NumberId { get; set; }
    public string? ConversationId { get; set; }
    public string? MessageId { get; set; }
    public MessageDirection Direction { get; set; }
    public UsageEventType EventType { get; set; }
    public string? AiProviderId { get; set; }
    public string? AiModel { get; set; }
    public int TokensIn { get; set; }
    public int TokensOut { get; set; }
    public decimal Cost { get; set; }
    public int LatencyMs { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorCode { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
