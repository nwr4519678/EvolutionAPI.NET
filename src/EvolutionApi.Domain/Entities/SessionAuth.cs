namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Session authentication credentials stored for an instance.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class SessionAuth
{
    public long Id { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public string SessionKey { get; set; } = string.Empty;
    public string EncryptedJson { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Instance? Instance { get; set; }
}
