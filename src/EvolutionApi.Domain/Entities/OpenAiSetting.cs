using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// OpenAI integration settings.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class OpenAiSetting : BaseEntity
{
    public string InstanceId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string? SystemPrompt { get; set; }
    public int MaxTokens { get; set; } = 1024;
    public double Temperature { get; set; } = 1.0;
    public bool SpeechToText { get; set; }

    // Navigation property
    public Instance? Instance { get; set; }
}
