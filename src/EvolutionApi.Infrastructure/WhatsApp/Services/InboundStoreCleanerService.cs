using EvolutionApi.Infrastructure.WhatsApp.Engine;

namespace EvolutionApi.Infrastructure.WhatsApp.Services;

/// <summary>
/// Background service that prunes expired entries from <see cref="WaInboundMessageStore"/>
/// every 30 seconds to prevent memory accumulation from unmatched decrypted messages.
/// </summary>
public sealed class InboundStoreCleanerService(
    WaInboundMessageStore store,
    ILogger<InboundStoreCleanerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Inbound message store cleaner started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            try { store.Prune(); }
            catch (Exception ex) { logger.LogWarning(ex, "Inbound store prune failed."); }
        }
    }
}
