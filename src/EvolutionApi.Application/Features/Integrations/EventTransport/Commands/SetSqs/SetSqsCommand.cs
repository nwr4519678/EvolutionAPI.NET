using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Integrations.EventTransport.Caching;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Integrations.EventTransport.Commands.SetSqs;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record SetSqsCommand(
    string    InstanceName,
    bool      Enabled         = false,
    string?   AccessKeyId     = null,
    string?   SecretAccessKey = null,
    string?   AccountId       = null,
    string?   Region          = null,
    string[]? Events          = null) : ICommand<object>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SetSqsCommandHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations,
    ICacheService cache) : MediatR.IRequestHandler<SetSqsCommand, object>
{
    private static readonly JsonSerializerOptions Jo = new(JsonSerializerDefaults.Web);

    public async Task<object> Handle(SetSqsCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var existing = await integrations.GetSqsAsync(instance.Id, ct)
            ?? new SqsSetting { InstanceId = instance.Id };

        existing.Enabled         = cmd.Enabled;
        existing.AccessKeyId     = cmd.AccessKeyId     ?? existing.AccessKeyId;
        existing.SecretAccessKey = cmd.SecretAccessKey ?? existing.SecretAccessKey;
        existing.AccountId       = cmd.AccountId       ?? existing.AccountId;
        existing.Region          = cmd.Region          ?? existing.Region;
        existing.EventsJson      = JsonSerializer.Serialize(cmd.Events ?? [], Jo);
        existing.UpdatedAt       = DateTime.UtcNow;

        await integrations.AddOrUpdateSqsAsync(existing, ct);
        await cache.RemoveByTagAsync(EventTransportCacheKeys.SqsTag(cmd.InstanceName), ct);

        // Mask secret key in response
        return new { instanceName = cmd.InstanceName, sqs = cmd with { SecretAccessKey = "****" } };
    }
}
