using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EvolutionApi.Infrastructure.RateLimiting;

/// <summary>
/// Infrastructure extensions for configuring application-wide rate limiting policies across all features.
/// </summary>
public static class RateLimitingExtensions
{
    public const string ApiPolicy = "api";
    public const string MessagePolicy = "message";
    public const string InstancePolicy = "instance";
    public const string GroupPolicy = "group";
    public const string ChatPolicy = "chat";
    public const string CallPolicy = "call";
    public const string LabelPolicy = "label";
    public const string IntegrationPolicy = "integration";
    public const string SettingsPolicy = "settings";

    /// <summary>
    /// Configures fine-grained rate limiters for all features partitioned by API Key or Client IP.
    /// </summary>
    public static IServiceCollection AddInfrastructureRateLimiting(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        var settings = new RateLimitingSettings();
        configuration?.GetSection(RateLimitingSettings.SectionName).Bind(settings);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.ContentType = "application/json";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                var problem = new
                {
                    statusCode = StatusCodes.Status429TooManyRequests,
                    error = "Too Many Requests",
                    message = "Rate limit exceeded. Please wait before retrying."
                };

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web)), ct);
            };

            // 1. General API policy (baseline for all endpoints)
            options.AddPolicy(ApiPolicy, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.ApiPermitLimit,
                        Window = TimeSpan.FromMinutes(settings.ApiWindowMinutes),
                        SegmentsPerWindow = settings.ApiSegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // 2. Message sending policy (WhatsApp outbound messages protection)
            options.AddPolicy(MessagePolicy, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.MessagePermitLimit,
                        Window = TimeSpan.FromMinutes(settings.MessageWindowMinutes),
                        SegmentsPerWindow = settings.MessageSegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // 3. Instance operations policy (Create, Connect, Disconnect, Delete)
            options.AddPolicy(InstancePolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.InstancePermitLimit,
                        Window = TimeSpan.FromMinutes(settings.InstanceWindowMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // 4. Groups feature policy
            options.AddPolicy(GroupPolicy, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.GroupPermitLimit,
                        Window = TimeSpan.FromMinutes(settings.GroupWindowMinutes),
                        SegmentsPerWindow = 4,
                        QueueLimit = 0
                    }));

            // 5. Chat feature policy
            options.AddPolicy(ChatPolicy, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.ChatPermitLimit,
                        Window = TimeSpan.FromMinutes(settings.ChatWindowMinutes),
                        SegmentsPerWindow = 4,
                        QueueLimit = 0
                    }));

            // 6. Calls feature policy
            options.AddPolicy(CallPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.CallPermitLimit,
                        Window = TimeSpan.FromMinutes(settings.CallWindowMinutes),
                        QueueLimit = 0
                    }));

            // 7. Labels feature policy
            options.AddPolicy(LabelPolicy, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.LabelPermitLimit,
                        Window = TimeSpan.FromMinutes(settings.LabelWindowMinutes),
                        SegmentsPerWindow = 4,
                        QueueLimit = 0
                    }));

            // 8. Integrations feature policy (Typebot, OpenAI, Chatwoot, EventTransport)
            options.AddPolicy(IntegrationPolicy, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.IntegrationPermitLimit,
                        Window = TimeSpan.FromMinutes(settings.IntegrationWindowMinutes),
                        SegmentsPerWindow = 4,
                        QueueLimit = 0
                    }));

            // 9. Settings feature policy (Settings, Proxy, Webhook)
            options.AddPolicy(SettingsPolicy, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.SettingsPermitLimit,
                        Window = TimeSpan.FromMinutes(settings.SettingsWindowMinutes),
                        SegmentsPerWindow = 4,
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("apikey", out var apiKey) && !string.IsNullOrWhiteSpace(apiKey))
            return $"key:{apiKey}";

        if (httpContext.Request.Headers.TryGetValue("X-Api-Key", out var xApiKey) && !string.IsNullOrWhiteSpace(xApiKey))
            return $"key:{xApiKey}";

        // Cloudflare real IP
        if (httpContext.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp) && !string.IsNullOrWhiteSpace(cfIp))
            return $"ip:{cfIp}";

        // Reverse proxy / IIS X-Forwarded-For
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var clientIp = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(clientIp))
                return $"ip:{clientIp}";
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrEmpty(ip) ? "anonymous" : $"ip:{ip}";
    }
}
