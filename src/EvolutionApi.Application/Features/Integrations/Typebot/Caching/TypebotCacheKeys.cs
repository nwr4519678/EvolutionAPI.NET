namespace EvolutionApi.Application.Features.Integrations.Typebot.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Typebot integration feature.
/// </summary>
public static class TypebotCacheKeys
{
    public static string Key(string instanceName) => $"integrations:typebot:{instanceName}";
    public static string Tag(string instanceName) => $"instance:{instanceName}:typebot";
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(15);
}
