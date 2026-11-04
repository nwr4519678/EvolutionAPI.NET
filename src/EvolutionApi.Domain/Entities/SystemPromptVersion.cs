using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

public sealed class SystemPromptVersion : BaseEntity
{
    public required string PromptId { get; set; }
    public required string TenantId { get; set; }
    public required string Content { get; set; }
    public int VersionNumber { get; set; }
    public string? Note { get; set; }
    public string? AuthorId { get; set; }
    public bool IsDraft { get; set; } = true;
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }

    // Navigation
    public SystemPrompt? Prompt { get; set; }
}
