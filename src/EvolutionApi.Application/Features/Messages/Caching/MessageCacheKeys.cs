namespace EvolutionApi.Application.Features.Messages.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Messages feature.
/// </summary>
public static class MessageCacheKeys
{
    public static string Tag(string instanceName) => $"instance:{instanceName}:messages";
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);
}
