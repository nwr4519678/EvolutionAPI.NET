using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Infrastructure.Services.EventTransport;

/// <summary>
/// Publishes WhatsApp events to RabbitMQ using MassTransit.RabbitMQ.
/// Per-instance configuration is loaded from the database at runtime,
/// so different WhatsApp instances can fan-out to different exchanges.
/// </summary>
public sealed class RabbitMqEventDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqEventDispatcher> logger)
{
    /// <summary>
    /// Publishes a WhatsApp event payload to the configured RabbitMQ exchange for the given instance.
    /// </summary>
    public async Task PublishAsync(string instanceId, string eventName, object payload, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var integrations = scope.ServiceProvider.GetRequiredService<IIntegrationRepository>();

        var cfg = await integrations.GetRabbitMqAsync(instanceId, ct);
        if (cfg is null || !cfg.Enabled || string.IsNullOrWhiteSpace(cfg.Uri))
        {
            logger.LogDebug("RabbitMQ not configured or disabled for instance {InstanceId}", instanceId);
            return;
        }

        try
        {
            // Build a one-off MassTransit bus pointed at this instance's broker URI
            var bus = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                x.Host(new Uri(cfg.Uri));
            });

            await bus.StartAsync(ct);
            try
            {
                var exchange = string.IsNullOrWhiteSpace(cfg.Exchange)
                    ? "evolution_exchange"
                    : cfg.Exchange;

                var endpoint = await bus.GetSendEndpoint(new Uri($"exchange:{exchange}"));
                await endpoint.Send(new WhatsAppEvent(instanceId, eventName, payload), ct);
                logger.LogInformation("RabbitMQ event published: {Event} for instance {InstanceId}", eventName, instanceId);
            }
            finally
            {
                await bus.StopAsync(ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish RabbitMQ event {Event} for instance {InstanceId}", eventName, instanceId);
        }
    }
}

/// <summary>Canonical event envelope published to all transports.</summary>
public sealed record WhatsAppEvent(string InstanceId, string EventName, object Payload);
