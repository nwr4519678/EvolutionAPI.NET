using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Typebot session state per remote JID.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class TypebotSession : BaseEntity
{
    public string InstanceId { get; set; } = string.Empty;
    public string RemoteJid { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Status { get; set; } = "opened";

    // Navigation property
    public Instance? Instance { get; set; }
}
