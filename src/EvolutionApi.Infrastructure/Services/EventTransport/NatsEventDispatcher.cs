using EvolutionApi.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using System.Text.Json;

namespace EvolutionApi.Infrastructure.Services.EventTransport;

/// <summary>
/// Publishes WhatsApp events to NATS using the official NATS.Net package.
/// Per-instance NATS URI is loaded from the database at runtime.
/// </summary>
public sealed class NatsEventDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<NatsEventDispatcher> logger)
{
    private static readonly JsonSerializerOptions Jo = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(string instanceId, string eventName, object payload, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var integrations = scope.ServiceProvider.GetRequiredService<IIntegrationRepository>();

        var cfg = await integrations.GetNatsAsync(instanceId, ct);
        if (cfg is null || !cfg.Enabled || string.IsNullOrWhiteSpace(cfg.Uri))
        {
            logger.LogDebug("NATS not configured or disabled for instance {InstanceId}", instanceId);
            return;
        }

        try
        {
            var opts = new NatsOpts { Url = cfg.Uri };
            await using var connection = new NatsConnection(opts);
            await connection.ConnectAsync();

            var subject = $"evolution.{instanceId}.{eventName}";
            var data = JsonSerializer.Serialize(new { instanceId, eventName, payload }, Jo);

            await connection.PublishAsync(subject, data, cancellationToken: ct);
            logger.LogInformation("NATS event published: {Subject}", subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish NATS event {Event} for instance {InstanceId}", eventName, instanceId);
        }
    }
}
