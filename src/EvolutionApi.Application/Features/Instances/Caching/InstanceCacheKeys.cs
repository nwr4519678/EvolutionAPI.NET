namespace EvolutionApi.Application.Features.Instances.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Instances feature.
/// </summary>
public static class InstanceCacheKeys
{
    public static string All(int page = 1, int pageSize = 20) => $"instances:all:p{page}:s{pageSize}";
    public static string ByName(string name) => $"instances:{name}";
    public const string Tag = "instances";
    public static readonly TimeSpan Expiration = TimeSpan.FromSeconds(30);
}
