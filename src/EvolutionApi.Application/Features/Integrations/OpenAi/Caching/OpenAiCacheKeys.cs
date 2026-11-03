namespace EvolutionApi.Application.Features.Integrations.OpenAi.Caching;

/// <summary>
/// Cache keys and invalidation tags for the OpenAI integration feature.
/// </summary>
public static class OpenAiCacheKeys
{
    public static string Key(string instanceName) => $"integrations:openai:{instanceName}";
    public static string Tag(string instanceName) => $"instance:{instanceName}:openai";
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(15);
}
