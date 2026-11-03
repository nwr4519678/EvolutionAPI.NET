namespace EvolutionApi.Application.Features.Settings.Caching;

/// <summary>
/// Cache keys and invalidation tags for the Settings feature.
/// </summary>
public static class SettingsCacheKeys
{
    public static string Key(string instanceName) => $"settings:{instanceName}";
    public static string Tag(string instanceName) => $"instance:{instanceName}:settings";
    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(15);
}
