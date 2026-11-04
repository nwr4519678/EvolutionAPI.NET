using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

public sealed class UsageDailyAggregate : BaseEntity
{
    public required string TenantId { get; set; }
    public required string NumberId { get; set; }
    public DateOnly Date { get; set; }
    public int MessagesIn { get; set; }
    public int MessagesOut { get; set; }
    public int AiCalls { get; set; }
    public long TokensIn { get; set; }
    public long TokensOut { get; set; }
    public decimal TotalCost { get; set; }
    public int AvgLatencyMs { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}
