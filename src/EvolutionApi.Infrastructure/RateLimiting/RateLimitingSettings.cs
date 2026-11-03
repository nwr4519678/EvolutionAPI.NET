namespace EvolutionApi.Infrastructure.RateLimiting;

/// <summary>
/// Rate limiting configuration settings with production defaults.
/// Can be overridden via the "RateLimiting" section in appsettings.json or environment variables.
/// </summary>
public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>General API sliding window rate limit (permits per window).</summary>
    public int ApiPermitLimit { get; set; } = 200;
    public int ApiWindowMinutes { get; set; } = 1;
    public int ApiSegmentsPerWindow { get; set; } = 6;

    /// <summary>Message sending sliding window rate limit (permits per window).</summary>
    public int MessagePermitLimit { get; set; } = 30;
    public int MessageWindowMinutes { get; set; } = 1;
    public int MessageSegmentsPerWindow { get; set; } = 6;

    /// <summary>Instance operations rate limit (permits per window).</summary>
    public int InstancePermitLimit { get; set; } = 15;
    public int InstanceWindowMinutes { get; set; } = 1;

    /// <summary>Groups feature rate limit.</summary>
    public int GroupPermitLimit { get; set; } = 40;
    public int GroupWindowMinutes { get; set; } = 1;

    /// <summary>Chat feature rate limit.</summary>
    public int ChatPermitLimit { get; set; } = 60;
    public int ChatWindowMinutes { get; set; } = 1;

    /// <summary>Calls feature rate limit.</summary>
    public int CallPermitLimit { get; set; } = 30;
    public int CallWindowMinutes { get; set; } = 1;

    /// <summary>Labels feature rate limit.</summary>
    public int LabelPermitLimit { get; set; } = 60;
    public int LabelWindowMinutes { get; set; } = 1;

    /// <summary>Integrations feature rate limit (Typebot, OpenAI, Chatwoot, EventTransport).</summary>
    public int IntegrationPermitLimit { get; set; } = 60;
    public int IntegrationWindowMinutes { get; set; } = 1;

    /// <summary>Settings, Proxy, Webhook feature rate limit.</summary>
    public int SettingsPermitLimit { get; set; } = 60;
    public int SettingsWindowMinutes { get; set; } = 1;
}
