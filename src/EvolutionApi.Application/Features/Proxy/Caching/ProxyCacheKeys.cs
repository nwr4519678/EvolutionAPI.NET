namespace EvolutionApi.Application.Features.Proxy.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Proxy feature.
/// </summary>
public static class ProxyCacheKeys
{
    public static string Key(string instanceName) => $"proxy:{instanceName}";
    public static string Tag(string instanceName) => $"instance:{instanceName}:proxy";
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(15);
}
