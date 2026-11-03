using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Proxy configuration entity for a WhatsApp instance.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class InstanceProxy : BaseEntity
{
    public string InstanceId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordEncrypted { get; set; } = string.Empty;

    // Navigation property
    public Instance? Instance { get; set; }
}
