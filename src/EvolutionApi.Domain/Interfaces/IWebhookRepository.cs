using EvolutionApi.Domain.Entities;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Webhook configuration repository contract.
/// </summary>
public interface IWebhookRepository : IRepository<WebhookConfig>
{
    Task<WebhookConfig?> GetByInstanceIdAsync(string instanceId, CancellationToken ct = default);
}
