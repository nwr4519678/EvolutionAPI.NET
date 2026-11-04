using EvolutionApi.Domain.Enums;
using EvolutionApi.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Infrastructure.WhatsApp.Services;

/// <summary>
/// Hosted background worker that concurrently boots Baileys connections on gateway startup
/// for all active tenant instances across isolated tenant boundaries.
/// Uses bounded parallelism to run efficiently on resource-constrained hosting (e.g. Monster ASP.NET).
/// </summary>
public sealed class MultiTenantWhatsAppBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    InstanceRuntimeManager runtime,
    IInstanceLifecycleFsmFactory fsmFactory,
    ILogger<MultiTenantWhatsAppBackgroundWorker> logger) : BackgroundService
{
    private const int MaxBootConcurrency = 4; // Monster ASP.NET hosting resource cap

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MultiTenantWhatsAppBackgroundWorker starting active instances bootstrap...");

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Query active instances across all tenants
            var allInstances = await uow.Instances.GetAllWithDetailsAsync(stoppingToken);
            var activeInstances = allInstances
                .Where(i => string.Equals(i.ConnectionStatus, "open", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(i.ConnectionStatus, "connecting", StringComparison.OrdinalIgnoreCase))
                .ToList();

            logger.LogInformation("Found {Count} active tenant WhatsApp instances requiring connection boot", activeInstances.Count);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxBootConcurrency,
                CancellationToken = stoppingToken
            };

            await Parallel.ForEachAsync(activeInstances, parallelOptions, async (instance, ct) =>
            {
                var tenantId = instance.TenantId ?? "default";
                logger.LogInformation("[{TenantId}] Bootstrapping WhatsApp instance '{InstanceName}' ({InstanceId})...",
                    tenantId, instance.Name, instance.Id);

                var fsm = fsmFactory.GetOrCreate(
                    tenantId,
                    instance.Id,
                    instance.Name,
                    InstanceLifecycleState.Initializing);

                try
                {
                    await fsm.FireAsync(InstanceTrigger.Initialize, ct);
                    await runtime.StartAsync(instance, ct);
                    logger.LogInformation("[{TenantId}] Successfully booted instance '{InstanceName}'",
                        tenantId, instance.Name);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    logger.LogWarning("[{TenantId}] Boot of '{InstanceName}' cancelled", tenantId, instance.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[{TenantId}] Failed to bootstrap instance '{InstanceName}'. Setting state to Disconnected",
                        tenantId, instance.Name);

                    try
                    {
                        await fsm.FireAsync(InstanceTrigger.FatalErrorOccurred, CancellationToken.None);
                    }
                    catch
                    {
                        // Ignore secondary FSM transition errors
                    }
                }
            });

            logger.LogInformation("MultiTenantWhatsAppBackgroundWorker bootstrap sequence completed successfully");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("MultiTenantWhatsAppBackgroundWorker startup cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error occurred during MultiTenant WhatsApp instances bootstrap");
        }
    }
}
