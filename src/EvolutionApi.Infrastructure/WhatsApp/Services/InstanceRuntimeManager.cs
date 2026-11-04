using System.Collections.Concurrent;
using System.Text.Json;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EvolutionApi.Infrastructure.WhatsApp.Services;

/// <summary>Owns the lifetime of one isolated provider connection per configured instance.</summary>
public sealed class InstanceRuntimeManager(IWhatsAppEngine engine)
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task StartAsync(Instance instance, CancellationToken cancellationToken)
    {
        var gate = _locks.GetOrAdd(instance.Id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try { await engine.StartAsync(instance.Id, instance.Name, instance.Number, cancellationToken); }
        finally { gate.Release(); }
    }

    public async Task StopAsync(string instanceId, CancellationToken cancellationToken)
    {
        var gate = _locks.GetOrAdd(instanceId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try { await engine.StopAsync(instanceId, cancellationToken); }
        finally { gate.Release(); }
    }

    public Task<JsonElement> ConnectAsync(string instanceId, string? number, CancellationToken cancellationToken)
        => engine.ConnectAsync(instanceId, number, cancellationToken);

    public Task<JsonElement> ExecuteOperationAsync(string instanceId, string operation, JsonElement payload, CancellationToken cancellationToken)
        => engine.SendAsync(instanceId, operation, payload, cancellationToken);

    public Task<string> GetStateAsync(string instanceId, CancellationToken cancellationToken)
        => engine.GetStateAsync(instanceId, cancellationToken);
}

public sealed class InstanceRecoveryService(
    IServiceScopeFactory scopeFactory,
    InstanceRuntimeManager runtime,
    ILogger<InstanceRecoveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var instances = await db.Instances.AsNoTracking().Where(i => i.ConnectionStatus == "open").ToListAsync(stoppingToken);
            foreach (var instance in instances)
            {
                try { await runtime.StartAsync(instance, stoppingToken); }
                catch (Exception ex) { logger.LogWarning(ex, "Could not recover WhatsApp instance {InstanceName}", instance.Name); }
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Instance recovery failed");
        }
    }
}
