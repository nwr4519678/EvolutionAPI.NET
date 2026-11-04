namespace EvolutionApi.Application.Features.Labels.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Labels feature.
/// </summary>
public static class LabelCacheKeys
{
    public static string All(string instanceName) => $"labels:{instanceName}:all";
    public static string Tag(string instanceName) => $"instance:{instanceName}:labels";
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(10);
}
