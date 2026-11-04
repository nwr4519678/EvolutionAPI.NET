namespace EvolutionApi.Application.Features.Groups.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Groups feature.
/// </summary>
public static class GroupCacheKeys
{
    public static string All(string instanceName) => $"groups:{instanceName}:all";
    public static string Info(string instanceName, string groupJid) => $"groups:{instanceName}:{groupJid}";
    public static string Tag(string instanceName) => $"instance:{instanceName}:groups";
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);
}
