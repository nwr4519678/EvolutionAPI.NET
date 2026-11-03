using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Webhook delivery tracking record.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class WebhookDelivery : BaseEntity
{
    public string InstanceName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string HeadersJson { get; set; } = "{}";
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeaseUntil { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? LastError { get; set; }
}
