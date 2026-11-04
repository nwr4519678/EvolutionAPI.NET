using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// RabbitMQ event publishing configuration.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class RabbitMqSetting : BaseEntity
{
    public string InstanceId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Uri { get; set; } = string.Empty;
    public string Exchange { get; set; } = "evolution_exchange";
    public bool GlobalEvents { get; set; }
    public string EventsJson { get; set; } = "[]";

    // Navigation property
    public Instance? Instance { get; set; }
}
