using EvolutionApi.Domain.Common;
using EvolutionApi.Domain.Enums;

namespace EvolutionApi.Domain.Entities;

public sealed class BotRule : BaseEntity
{
    public required string TenantId { get; set; }
    public string? NumberId { get; set; } // null = all numbers for this tenant
    public required string Name { get; set; }
    public BotRuleType RuleType { get; set; }
    public int Priority { get; set; } = 100;
    public string ConfigJson { get; set; } = "{}"; // rule-specific JSON config
    public bool IsEnabled { get; set; } = true;
    public bool IsDeleted { get; set; }
}
