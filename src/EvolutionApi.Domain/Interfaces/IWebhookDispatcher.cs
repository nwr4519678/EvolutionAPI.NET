using System.Text.Json;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Webhook dispatcher contract for sending outbound event envelopes.
/// </summary>
public interface IWebhookDispatcher
{
    Task DispatchAsync(string instanceName, string eventName, JsonElement payload, CancellationToken ct = default);
}
