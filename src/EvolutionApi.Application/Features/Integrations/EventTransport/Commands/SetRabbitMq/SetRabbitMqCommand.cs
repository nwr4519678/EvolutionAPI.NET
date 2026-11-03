using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Integrations.EventTransport.Caching;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Integrations.EventTransport.Commands.SetRabbitMq;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record SetRabbitMqCommand(
    string    InstanceName,
    bool      Enabled      = false,
    string?   Uri          = null,
    string?   Exchange     = null,
    bool?     GlobalEvents = null,
    string[]? Events       = null) : ICommand<object>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SetRabbitMqCommandHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations,
    ICacheService cache) : MediatR.IRequestHandler<SetRabbitMqCommand, object>
{
    private static readonly JsonSerializerOptions Jo = new(JsonSerializerDefaults.Web);

    public async Task<object> Handle(SetRabbitMqCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var existing = await integrations.GetRabbitMqAsync(instance.Id, ct)
            ?? new RabbitMqSetting { InstanceId = instance.Id };

        existing.Enabled      = cmd.Enabled;
        existing.Uri          = cmd.Uri          ?? existing.Uri;
        existing.Exchange     = cmd.Exchange     ?? existing.Exchange;
        existing.GlobalEvents = cmd.GlobalEvents ?? existing.GlobalEvents;
        existing.EventsJson   = JsonSerializer.Serialize(cmd.Events ?? [], Jo);
        existing.UpdatedAt    = DateTime.UtcNow;

        await integrations.AddOrUpdateRabbitMqAsync(existing, ct);
        await cache.RemoveByTagAsync(EventTransportCacheKeys.RabbitMqTag(cmd.InstanceName), ct);

        return new { instanceName = cmd.InstanceName, rabbitmq = cmd };
    }
}
