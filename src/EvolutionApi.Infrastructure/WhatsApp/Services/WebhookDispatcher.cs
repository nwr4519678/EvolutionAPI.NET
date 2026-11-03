using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using EvolutionApi.Infrastructure.Persistence;
using EvolutionApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EvolutionApi.Infrastructure.WhatsApp.Services;

/// <summary>Persists webhook envelopes and returns after durable enqueue; HTTP delivery runs in the background.</summary>
public sealed class WebhookDispatcher(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    WebhookDeliveryQueue queue,
    WebSocketTransport wsTransport,
    OutboundUrlValidator outboundUrls) : IWebhookDispatcher
{
    public async Task DispatchAsync(string instanceName, string eventName, JsonElement data, CancellationToken ct = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hook = await db.WebhookConfigs.AsNoTracking().Include(x => x.Instance)
            .SingleOrDefaultAsync(x => x.Instance != null && x.Instance.Name == instanceName, ct);
        var normalizedEvent = NormalizeEvent(eventName);
        var eventSlug = normalizedEvent.Replace('_', '-').ToLowerInvariant();
        var localEnabled = hook is { Enabled: true } &&
                           (JsonSerializer.Deserialize<string[]>(hook.EventsJson) ?? []).Contains(normalizedEvent, StringComparer.OrdinalIgnoreCase) &&
                           await outboundUrls.IsAllowedAsync(hook.Url, ct);
        var globalUrl = GetConfig("WEBHOOK_GLOBAL_URL", "Evolution:GlobalWebhook:Url", string.Empty);
        var globalEnabled = GetBoolConfig("WEBHOOK_GLOBAL_ENABLED", "Evolution:GlobalWebhook:Enabled", false) &&
                            IsGlobalEventEnabled(normalizedEvent) && await outboundUrls.IsAllowedAsync(globalUrl, ct);
        if (!localEnabled && !globalEnabled) return;

        var serverUrl = configuration["Evolution:ServerUrl"];
        if (string.IsNullOrWhiteSpace(serverUrl)) serverUrl = configuration["SERVER_URL"];
        var payloadDestination = hook is not null && !string.IsNullOrWhiteSpace(hook.Url)
            ? hook.Url
            : globalUrl.TrimEnd('/') + "/" + eventSlug;
        var envelope = new Dictionary<string, object?>
        {
            ["event"] = eventName,
            ["instance"] = instanceName,
            ["data"] = data,
            ["destination"] = payloadDestination,
            ["date_time"] = DateTimeOffset.UtcNow,
            ["sender"] = serverUrl,
            ["server_url"] = serverUrl,
            ["apikey"] = configuration["Evolution:ApiKey"]
        };

        var now = DateTime.UtcNow;
        if (localEnabled)
        {
            var localHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(hook!.HeadersJson) ?? [];
            AddJwtAuthorization(localHeaders);
            var target = hook.ByEvents ? AppendEventPath(hook.Url, eventSlug) : hook.Url;
            db.WebhookDeliveries.Add(CreateDelivery(instanceName, eventName, target, localHeaders, envelope, now));
        }
        if (globalEnabled)
        {
            var byEvents = GetBoolConfig("WEBHOOK_GLOBAL_WEBHOOK_BY_EVENTS", "Evolution:GlobalWebhook:ByEvents", false);
            var target = byEvents ? AppendEventPath(globalUrl, eventSlug) : globalUrl;
            db.WebhookDeliveries.Add(CreateDelivery(instanceName, eventName, target, new Dictionary<string, string>(), envelope, now));
        }
        await db.SaveChangesAsync(ct);
        foreach (var row in db.ChangeTracker.Entries<WebhookDelivery>().Where(x => x.State == EntityState.Unchanged).Select(x => x.Entity))
            queue.Notify(row.Id);

        // Broadcast to WebSocket subscribers (fire-and-forget; don't block dispatch)
        _ = Task.Run(async () =>
        {
            try { await wsTransport.BroadcastAsync(instanceName, eventName, data, CancellationToken.None); }
            catch { /* never throw from fire-and-forget */ }
        }, CancellationToken.None);
    }

    private static WebhookDelivery CreateDelivery(string instanceName, string eventName, string target,
        Dictionary<string, string> headers, Dictionary<string, object?> envelope, DateTime now) => new()
    {
        InstanceName = instanceName,
        EventName = eventName,
        Destination = target,
        HeadersJson = JsonSerializer.Serialize(headers),
        PayloadJson = JsonSerializer.Serialize(envelope),
        CreatedAt = now,
        NextAttemptAt = now
    };

    private static string AppendEventPath(string url, string eventSlug) => url.TrimEnd('/') + "/" + eventSlug;

    private bool IsGlobalEventEnabled(string eventName)
    {
        var envKey = eventName switch { "GROUP_UPDATE" => "WEBHOOK_EVENTS_GROUPS_UPDATE", _ => "WEBHOOK_EVENTS_" + eventName };
        return GetBoolConfig(envKey, "Evolution:GlobalWebhook:Events:" + eventName, false);
    }

    private string GetConfig(string environmentKey, string appSettingsKey, string defaultValue) =>
        configuration[environmentKey] ?? configuration[appSettingsKey] ?? defaultValue;

    private bool GetBoolConfig(string environmentKey, string appSettingsKey, bool defaultValue) =>
        bool.TryParse(configuration[environmentKey] ?? configuration[appSettingsKey], out var value) ? value : defaultValue;

    private static string NormalizeEvent(string eventName) => eventName.Replace('.', '_').Replace('-', '_').ToUpperInvariant();

    private static void AddJwtAuthorization(IDictionary<string, string> headers)
    {
        if (!headers.Remove("jwt_key", out var signingKey) || string.IsNullOrWhiteSpace(signingKey)) return;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { iat = now, exp = now + 600, app = "evolution", action = "webhook" }));
        var content = header + "." + payload;
        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey), Encoding.ASCII.GetBytes(content));
        headers["Authorization"] = "Bearer " + content + "." + Base64Url(signature);
    }

    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

public sealed class WebhookDeliveryQueue
{
    private readonly Channel<string> _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false
    });

    public void Notify(string id) => _channel.Writer.TryWrite(id);
    internal bool TryRead(out string? id) => _channel.Reader.TryRead(out id);
    internal ValueTask<bool> WaitToReadAsync(CancellationToken ct) => _channel.Reader.WaitToReadAsync(ct);
}

public sealed class WebhookDeliveryWorker(
    WebhookDeliveryQueue queue,
    IServiceScopeFactory scopes,
    IHttpClientFactory clients,
    IConfiguration configuration,
    OutboundUrlValidator outboundUrls,
    ILogger<WebhookDeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string? id = null;
            try
            {
                if (!queue.TryRead(out id)) id = await FindNextAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Failed to query pending webhook deliveries from database. Retrying in 5s...");
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (OperationCanceledException) { break; }
                continue;
            }

            if (id is null)
            {
                try
                {
                    using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    var channelWait = queue.WaitToReadAsync(waitCts.Token).AsTask();
                    var timer = Task.Delay(PollInterval, stoppingToken);
                    if (await Task.WhenAny(channelWait, timer) == channelWait)
                        await channelWait;
                    else
                    {
                        waitCts.Cancel();
                        try { await channelWait; }
                        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested) { }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                continue;
            }

            try
            {
                await DeliverAsync(id, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Error occurred delivering webhook {Id}", id);
            }
        }
    }


    private async Task<string?> FindNextAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        return await db.WebhookDeliveries.AsNoTracking()
            .Where(x => (x.Status == "Pending" && x.NextAttemptAt <= now) || (x.Status == "Processing" && x.LeaseUntil <= now))
            .OrderBy(x => x.CreatedAt).Select(x => x.Id).FirstOrDefaultAsync(ct);
    }

    private async Task DeliverAsync(string id, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var claimed = await db.WebhookDeliveries.Where(x => x.Id == id &&
                ((x.Status == "Pending" && x.NextAttemptAt <= now) || (x.Status == "Processing" && x.LeaseUntil <= now)))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "Processing")
                .SetProperty(x => x.LeaseUntil, now.Add(LeaseDuration))
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), ct);
        if (claimed == 0) return;

        var row = await db.WebhookDeliveries.SingleAsync(x => x.Id == id, ct);
        try
        {
            if (!await outboundUrls.IsAllowedAsync(row.Destination, ct))
            {
                await RecordFailureAsync(db, row, "Webhook destination is not an allowed public HTTP(S) address.", true, ct);
                return;
            }
            using var request = new HttpRequestMessage(HttpMethod.Post, row.Destination)
            {
                Content = new StringContent(row.PayloadJson, Encoding.UTF8, "application/json")
            };
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(row.HeadersJson) ?? [];
            foreach (var (name, value) in headers)
            {
                if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                if (!request.Headers.TryAddWithoutValidation(name, value))
                    request.Content.Headers.TryAddWithoutValidation(name, value);
            }

            var http = clients.CreateClient("webhooks");
            http.Timeout = TimeSpan.FromMilliseconds(Math.Clamp(GetIntConfig("WEBHOOK_REQUEST_TIMEOUT_MS", "Evolution:WebhookRetry:TimeoutMs", 30000), 1000, 600000));
            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                row.Status = "Delivered";
                row.DeliveredAt = DateTime.UtcNow;
                row.LeaseUntil = null;
                row.LastError = null;
                await db.SaveChangesAsync(ct);
                return;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            var error = $"Webhook returned HTTP {(int)response.StatusCode}: {errorBody}";
            var nonRetryable = GetNonRetryableStatusCodes().Contains((int)response.StatusCode);
            await RecordFailureAsync(db, row, error, nonRetryable, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The delivery lease expires and is reclaimable after the host restarts.
        }
        catch (Exception exception)
        {
            await RecordFailureAsync(db, row, exception.ToString(), false, CancellationToken.None);
        }
    }

    private async Task RecordFailureAsync(AppDbContext db, WebhookDelivery row, string error, bool nonRetryable, CancellationToken ct)
    {
        row.LastError = error;
        row.LeaseUntil = null;
        var maxAttempts = Math.Clamp(GetIntConfig("WEBHOOK_RETRY_MAX_ATTEMPTS", "Evolution:WebhookRetry:MaxAttempts", 10), 1, 100);
        if (nonRetryable || row.AttemptCount >= maxAttempts)
        {
            row.Status = "Failed";
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Webhook delivery {DeliveryId} permanently failed after {Attempts} attempts: {Error}", row.Id, row.AttemptCount, error);
            return;
        }

        var initial = Math.Clamp(GetIntConfig("WEBHOOK_RETRY_INITIAL_DELAY_SECONDS", "Evolution:WebhookRetry:InitialDelaySeconds", 5), 1, 3600);
        var maximum = Math.Clamp(GetIntConfig("WEBHOOK_RETRY_MAX_DELAY_SECONDS", "Evolution:WebhookRetry:MaxDelaySeconds", 300), initial, 86400);
        var exponential = GetBoolConfig("WEBHOOK_RETRY_USE_EXPONENTIAL_BACKOFF", "Evolution:WebhookRetry:UseExponentialBackoff", true);
        var delay = exponential ? Math.Min(maximum, initial * Math.Pow(2, Math.Min(row.AttemptCount - 1, 20))) : initial;
        var jitterFactor = Math.Clamp(GetDoubleConfig("WEBHOOK_RETRY_JITTER_FACTOR", "Evolution:WebhookRetry:JitterFactor", 0.2), 0, 1);
        delay = Math.Max(initial, delay + delay * jitterFactor * (Random.Shared.NextDouble() * 2 - 1));
        row.Status = "Pending";
        row.NextAttemptAt = DateTime.UtcNow.AddSeconds(delay);
        await db.SaveChangesAsync(ct);
        logger.LogWarning("Webhook delivery {DeliveryId} failed on attempt {Attempt}; retrying in {DelaySeconds:F1}s: {Error}", row.Id, row.AttemptCount, delay, error);
    }

    private HashSet<int> GetNonRetryableStatusCodes()
    {
        var configured = configuration["WEBHOOK_RETRY_NON_RETRYABLE_STATUS_CODES"] ?? configuration["Evolution:WebhookRetry:NonRetryableStatusCodes"];
        if (string.IsNullOrWhiteSpace(configured)) return [400, 401, 403, 404, 422];
        return configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var statusCode) ? statusCode : -1).Where(x => x >= 100).ToHashSet();
    }

    private int GetIntConfig(string environmentKey, string appSettingsKey, int defaultValue) =>
        int.TryParse(configuration[environmentKey] ?? configuration[appSettingsKey], out var value) ? value : defaultValue;

    private double GetDoubleConfig(string environmentKey, string appSettingsKey, double defaultValue) =>
        double.TryParse(configuration[environmentKey] ?? configuration[appSettingsKey], System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value : defaultValue;

    private bool GetBoolConfig(string environmentKey, string appSettingsKey, bool defaultValue) =>
        bool.TryParse(configuration[environmentKey] ?? configuration[appSettingsKey], out var value) ? value : defaultValue;
}

public sealed class WebhookService(AppDbContext db)
{
    public async Task<object> SetAsync(string instanceName, WebhookRequest request, CancellationToken ct)
    {
        var input = request.Webhook ?? throw new ArgumentException("webhook is required");
        if (string.IsNullOrWhiteSpace(input.Url)) throw new ArgumentException("webhook.url is required");
        var instance = await db.Instances.SingleOrDefaultAsync(x => x.Name == instanceName, ct)
                       ?? throw new KeyNotFoundException($"The '{instanceName}' instance does not exist");
        var invalidEvent = input.Events?.FirstOrDefault(x => !EvolutionEvents.Default.Contains(x, StringComparer.Ordinal));
        if (invalidEvent is not null) throw new ArgumentException($"'{invalidEvent}' is not a supported webhook event.");
        var hook = await db.WebhookConfigs.SingleOrDefaultAsync(x => x.InstanceId == instance.Id, ct);
        if (hook is null)
        {
            hook = new WebhookConfig { InstanceId = instance.Id };
            db.WebhookConfigs.Add(hook);
        }
        var events = !input.Enabled ? Array.Empty<string>() : input.Events is { Length: > 0 } ? input.Events : EvolutionEvents.Default;
        hook.Enabled = input.Enabled;
        hook.Url = input.Url;
        hook.EventsJson = JsonSerializer.Serialize(events);
        hook.HeadersJson = JsonSerializer.Serialize(input.Headers ?? new Dictionary<string, string>());
        hook.ByEvents = input.ByEvents ?? false;
        hook.Base64 = input.Base64 ?? false;
        hook.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToEvolutionShape(hook);
    }

    public async Task<object?> FindAsync(string instanceName, CancellationToken ct)
    {
        var hook = await db.WebhookConfigs.AsNoTracking().SingleOrDefaultAsync(x => x.Instance != null && x.Instance.Name == instanceName, ct);
        return hook is null ? null : ToEvolutionShape(hook);
    }

    private static object ToEvolutionShape(WebhookConfig hook) => new
    {
        id = hook.Id,
        enabled = hook.Enabled,
        url = hook.Url,
        events = JsonSerializer.Deserialize<string[]>(hook.EventsJson) ?? [],
        headers = JsonSerializer.Deserialize<Dictionary<string, string>>(hook.HeadersJson) ?? [],
        webhookBase64 = hook.Base64,
        webhookByEvents = hook.ByEvents,
        instanceId = hook.InstanceId,
        createdAt = hook.CreatedAt,
        updatedAt = hook.UpdatedAt
    };
}

public sealed record WebhookRequest(WebhookSettings? Webhook);
public sealed record WebhookSettings(bool Enabled, string? Url, string[]? Events, Dictionary<string, string>? Headers, bool? ByEvents, bool? Base64);

public static class EvolutionEvents
{
    public static readonly string[] Default = [
        "APPLICATION_STARTUP", "QRCODE_UPDATED", "MESSAGES_SET", "MESSAGES_UPSERT", "MESSAGES_EDITED", "MESSAGES_UPDATE", "MESSAGES_DELETE",
        "SEND_MESSAGE", "SEND_MESSAGE_UPDATE", "CONTACTS_SET", "CONTACTS_UPSERT", "CONTACTS_UPDATE", "PRESENCE_UPDATE", "CHATS_SET", "CHATS_UPSERT",
        "CHATS_UPDATE", "CHATS_DELETE", "GROUPS_UPSERT", "GROUP_UPDATE", "GROUP_PARTICIPANTS_UPDATE", "CONNECTION_UPDATE", "LABELS_EDIT",
        "LABELS_ASSOCIATION", "CALL", "TYPEBOT_START", "TYPEBOT_CHANGE_STATUS", "REMOVE_INSTANCE", "LOGOUT_INSTANCE", "INSTANCE_CREATE", "INSTANCE_DELETE", "STATUS_INSTANCE"];
}
