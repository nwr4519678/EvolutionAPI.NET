using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

public sealed class PlatformApiKey : BaseEntity
{
    public required string TenantId { get; set; }
    public required string Name { get; set; }
    public required string KeyHash { get; set; } // SHA-256 hash
    public string KeyPrefix { get; set; } = string.Empty; // first 8 chars for display
    public string[] Scopes { get; set; } = [];
    public string[]? AllowedIps { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
}
