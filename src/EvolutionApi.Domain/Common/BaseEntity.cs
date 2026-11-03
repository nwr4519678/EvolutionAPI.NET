namespace EvolutionApi.Domain.Common;

/// <summary>
/// Base entity with strongly-typed identifier and UTC audit timestamps.
/// All Id values are GUID-format strings generated at construction time.
/// Pure POCO — no framework dependencies, no business logic.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Unique entity identifier (GUID format: "D").</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("D");

    /// <summary>Timestamp when the entity was first persisted (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp of the most recent update (UTC). Must be set by callers on mutation.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
