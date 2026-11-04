using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

public sealed class SystemPrompt : BaseEntity
{
    public required string TenantId { get; set; }
    public string? NumberId { get; set; } // null = applies to entire tenant
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string CurrentVersionId { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }

    // Navigation
    public ICollection<SystemPromptVersion> Versions { get; set; } = [];
}
