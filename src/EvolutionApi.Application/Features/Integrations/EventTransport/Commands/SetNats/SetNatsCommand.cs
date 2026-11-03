using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Integrations.EventTransport.Caching;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Integrations.EventTransport.Commands.SetNats;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record SetNatsCommand(
    string    InstanceName,
    bool      Enabled = false,
    string?   Uri     = null,
    string[]? Events  = null) : ICommand<object>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SetNatsCommandHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations,
    ICacheService cache) : MediatR.IRequestHandler<SetNatsCommand, object>
{
    private static readonly JsonSerializerOptions Jo = new(JsonSerializerDefaults.Web);

    public async Task<object> Handle(SetNatsCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var existing = await integrations.GetNatsAsync(instance.Id, ct)
            ?? new NatsSetting { InstanceId = instance.Id };

        existing.Enabled    = cmd.Enabled;
        existing.Uri        = cmd.Uri ?? existing.Uri;
        existing.EventsJson = JsonSerializer.Serialize(cmd.Events ?? [], Jo);
        existing.UpdatedAt  = DateTime.UtcNow;

        await integrations.AddOrUpdateNatsAsync(existing, ct);
        await cache.RemoveByTagAsync(EventTransportCacheKeys.NatsTag(cmd.InstanceName), ct);

        return new { instanceName = cmd.InstanceName, nats = cmd };
    }
}
