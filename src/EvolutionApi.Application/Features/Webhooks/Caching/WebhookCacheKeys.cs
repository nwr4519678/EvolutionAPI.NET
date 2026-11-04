namespace EvolutionApi.Application.Features.Webhooks.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Webhooks feature.
/// </summary>
public static class WebhookCacheKeys
{
    public static string Key(string instanceName) => $"webhook:{instanceName}";
    public static string Tag(string instanceName) => $"instance:{instanceName}:webhook";
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(15);
}
