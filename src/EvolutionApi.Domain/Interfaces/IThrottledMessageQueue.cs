using System.Text.Json;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Per-tenant in-memory rate-throttling queue contract.
/// Enforces safe 3-6 second delays between WhatsApp outbound messages per tenant/instance.
/// </summary>
public interface IThrottledMessageQueue
{
    Task<JsonElement> EnqueueAndAwaitAsync(
        string tenantId,
        string instanceId,
        string instanceName,
        string operation,
        JsonElement payload,
        CancellationToken ct);
}
