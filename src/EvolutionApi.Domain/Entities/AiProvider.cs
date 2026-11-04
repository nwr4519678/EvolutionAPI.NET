using EvolutionApi.Domain.Common;
using EvolutionApi.Domain.Enums;

namespace EvolutionApi.Domain.Entities;

public sealed class AiProvider : BaseEntity
{
    public string? TenantId { get; set; } // null = platform-level shared provider
    public required string Name { get; set; }
    public AiProviderType ProviderType { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKeyEncrypted { get; set; } // encrypted at rest
    public required string DefaultModel { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2048;
    public int TimeoutSeconds { get; set; } = 30;
    public decimal PricePerKInputTokens { get; set; }
    public decimal PricePerKOutputTokens { get; set; }
    public int Priority { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;
    public bool IsDeleted { get; set; }
}
