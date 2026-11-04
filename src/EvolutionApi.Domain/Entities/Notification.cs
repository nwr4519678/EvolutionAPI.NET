using EvolutionApi.Domain.Common;
using EvolutionApi.Domain.Enums;

namespace EvolutionApi.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public required string TenantId { get; set; }
    public string? UserId { get; set; } // null = all tenant users
    public NotificationType Type { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
