using System.Collections.Concurrent;
using EvolutionApi.Domain.Enums;
using EvolutionApi.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stateless;

namespace EvolutionApi.Infrastructure.WhatsApp.State;

/// <summary>
/// Thread-safe Finite State Machine managing the lifecycle of a single WhatsApp instance.
/// Uses the 'Stateless' library to enforce strict, deterministic state transitions.
/// </summary>
public sealed class InstanceLifecycleFsm : IInstanceLifecycleFsm
{
    private readonly StateMachine<InstanceLifecycleState, InstanceTrigger> _fsm;
    private readonly string _tenantId;
    private readonly string _instanceId;
    private readonly string _instanceName;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public InstanceLifecycleState CurrentState => _fsm.State;
    public Task<IEnumerable<InstanceTrigger>> GetPermittedTriggersAsync() => _fsm.GetPermittedTriggersAsync();

    public InstanceLifecycleFsm(
        string tenantId,
        string instanceId,
        string instanceName,
        InstanceLifecycleState initialState,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        _tenantId = tenantId;
        _instanceId = instanceId;
        _instanceName = instanceName;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _fsm = new StateMachine<InstanceLifecycleState, InstanceTrigger>(initialState);

        ConfigureStateMachine();
    }

    private void ConfigureStateMachine()
    {
        _fsm.Configure(InstanceLifecycleState.Disconnected)
            .Permit(InstanceTrigger.Initialize, InstanceLifecycleState.Initializing);

        _fsm.Configure(InstanceLifecycleState.Initializing)
            .Permit(InstanceTrigger.QrGenerated, InstanceLifecycleState.GeneratingQr)
            .Permit(InstanceTrigger.CredentialsAccepted, InstanceLifecycleState.Connecting)
            .Permit(InstanceTrigger.FatalErrorOccurred, InstanceLifecycleState.Disconnected);

        _fsm.Configure(InstanceLifecycleState.GeneratingQr)
            .PermitReentry(InstanceTrigger.QrGenerated)
            .Permit(InstanceTrigger.CredentialsAccepted, InstanceLifecycleState.Connecting)
            .Permit(InstanceTrigger.DisconnectRequested, InstanceLifecycleState.Disconnected)
            .Permit(InstanceTrigger.FatalErrorOccurred, InstanceLifecycleState.Disconnected);

        _fsm.Configure(InstanceLifecycleState.Connecting)
            .Permit(InstanceTrigger.CredentialsAccepted, InstanceLifecycleState.Connected)
            .Permit(InstanceTrigger.ConnectionLost, InstanceLifecycleState.Retrying)
            .Permit(InstanceTrigger.DisconnectRequested, InstanceLifecycleState.Disconnected)
            .Permit(InstanceTrigger.FatalErrorOccurred, InstanceLifecycleState.Disconnected);

        _fsm.Configure(InstanceLifecycleState.Connected)
            .Permit(InstanceTrigger.DisconnectRequested, InstanceLifecycleState.Disconnected)
            .Permit(InstanceTrigger.ConnectionLost, InstanceLifecycleState.Retrying);

        _fsm.Configure(InstanceLifecycleState.Retrying)
            .Permit(InstanceTrigger.CredentialsAccepted, InstanceLifecycleState.Connected)
            .Permit(InstanceTrigger.DisconnectRequested, InstanceLifecycleState.Disconnected)
            .Permit(InstanceTrigger.FatalErrorOccurred, InstanceLifecycleState.Disconnected);

        _fsm.OnTransitionedAsync(async transition =>
        {
            _logger.LogInformation(
                "[{TenantId}] Instance '{InstanceName}' FSM transition: {Source} -> {Destination} via {Trigger}",
                _tenantId, _instanceName, transition.Source, transition.Destination, transition.Trigger);

            await PersistStateChangeAsync(transition.Destination);
            await BroadcastStateChangeAsync(transition.Destination);
        });
    }

    public bool CanFire(InstanceTrigger trigger) => _fsm.CanFire(trigger);

    public async Task FireAsync(InstanceTrigger trigger, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_fsm.CanFire(trigger))
            {
                await _fsm.FireAsync(trigger);
            }
            else
            {
                _logger.LogWarning(
                    "[{TenantId}] Cannot fire trigger '{Trigger}' from current state '{CurrentState}' for instance '{InstanceName}'",
                    _tenantId, trigger, _fsm.State, _instanceName);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task PersistStateChangeAsync(InstanceLifecycleState newState)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var instance = await uow.Instances.GetByIdAsync(_instanceId);
            if (instance is not null)
            {
                instance.ConnectionStatus = newState.ToString().ToLowerInvariant();
                instance.UpdatedAt = DateTime.UtcNow;
                uow.Instances.Update(instance);
                await uow.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist FSM state for instance {InstanceName}", _instanceName);
        }
    }

    private async Task BroadcastStateChangeAsync(InstanceLifecycleState newState)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notifications = scope.ServiceProvider.GetRequiredService<IEventNotificationService>();
            var statusStr = newState.ToString().ToLowerInvariant();

            await notifications.NotifyTenantConnectionStatusAsync(_tenantId, _instanceName, statusStr);
            await notifications.NotifyTenantEventAsync(_tenantId, _instanceName, "CONNECTION_UPDATE", new
            {
                instance = _instanceName,
                state = statusStr,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast FSM state change for instance {InstanceName}", _instanceName);
        }
    }
}

public sealed class InstanceLifecycleFsmFactory(
    IServiceScopeFactory scopeFactory,
    ILogger<InstanceLifecycleFsm> logger) : IInstanceLifecycleFsmFactory
{
    private readonly ConcurrentDictionary<string, InstanceLifecycleFsm> _machines = new(StringComparer.Ordinal);

    public IInstanceLifecycleFsm GetOrCreate(
        string tenantId,
        string instanceId,
        string instanceName,
        InstanceLifecycleState initialState)
    {
        return _machines.GetOrAdd(instanceId, _ => new InstanceLifecycleFsm(
            tenantId,
            instanceId,
            instanceName,
            initialState,
            scopeFactory,
            logger));
    }
}
