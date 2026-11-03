namespace EvolutionApi.Application.Features.Chat.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Chat feature.
/// </summary>
public static class ChatCacheKeys
{
    public static string ProfilePicture(string instanceName, string target) => $"chat:{instanceName}:profile-pic:{target}";
    public static string PrivacySettings(string instanceName) => $"chat:{instanceName}:privacy-settings";
    public static string Tag(string instanceName) => $"instance:{instanceName}:chat";
    public static readonly TimeSpan Expiration = TimeSpan.FromHours(1);
}
