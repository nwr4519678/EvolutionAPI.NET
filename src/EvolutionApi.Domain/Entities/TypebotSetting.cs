using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Typebot integration settings.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class TypebotSetting : BaseEntity
{
    public string InstanceId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? TypebotId { get; set; }
    public int Expire { get; set; }
    public string? KeywordFinish { get; set; }
    public int DelayMessage { get; set; } = 1000;
    public string? UnknownMessage { get; set; }
    public bool ListeningFromMe { get; set; }

    // Navigation property
    public Instance? Instance { get; set; }
}
