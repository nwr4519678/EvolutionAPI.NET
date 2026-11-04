namespace EvolutionApi.Application.Features.Calls.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Calls feature.
/// </summary>
public static class CallCacheKeys
{
    public static string Tag(string instanceName) => $"instance:{instanceName}:calls";
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);
}
