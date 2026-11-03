namespace EvolutionApi.Application.Features.Integrations.Chatwoot.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Chatwoot integration feature.
/// </summary>
public static class ChatwootCacheKeys
{
    public static string Key(string instanceName) => $"integrations:chatwoot:{instanceName}";
    public static string Tag(string instanceName) => $"instance:{instanceName}:chatwoot";
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(15);
}
