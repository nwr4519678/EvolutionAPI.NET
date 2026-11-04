using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Before { get; set; } // JSON
    public string? After { get; set; } // JSON
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
