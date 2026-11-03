using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Webhook configuration entity.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class WebhookConfig : BaseEntity
{
    public string InstanceId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Url { get; set; } = string.Empty;
    public string EventsJson { get; set; } = "[]";
    public string HeadersJson { get; set; } = "{}";
    public bool ByEvents { get; set; }
    public bool Base64 { get; set; }

    // Navigation property
    public Instance? Instance { get; set; }
}
