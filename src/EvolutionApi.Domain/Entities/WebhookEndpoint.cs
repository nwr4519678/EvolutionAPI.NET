using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

public sealed class WebhookEndpoint : BaseEntity
{
    public required string TenantId { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public string[] Events { get; set; } = [];
    public string? SecretEncrypted { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public bool IsDeleted { get; set; }

    // Navigation
    public ICollection<PlatformWebhookDelivery> Deliveries { get; set; } = [];
}
