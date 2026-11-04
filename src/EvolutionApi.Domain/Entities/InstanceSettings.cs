using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Settings entity for a WhatsApp instance.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class InstanceSettings : BaseEntity
{
    public string InstanceId { get; set; } = string.Empty;
    public bool RejectCall { get; set; }
    public string MsgCall { get; set; } = string.Empty;
    public bool GroupsIgnore { get; set; }
    public bool AlwaysOnline { get; set; }
    public bool ReadMessages { get; set; }
    public bool ReadStatus { get; set; }
    public bool SyncFullHistory { get; set; }
    public string WavoipToken { get; set; } = string.Empty;

    // Navigation property
    public Instance? Instance { get; set; }
}
