using EvolutionApi.Domain.Enums;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Contract for managing the Stateless Finite State Machine of a WhatsApp instance.
/// </summary>
public interface IInstanceLifecycleFsm
{
    InstanceLifecycleState CurrentState { get; }

    bool CanFire(InstanceTrigger trigger);

    Task FireAsync(InstanceTrigger trigger, CancellationToken ct = default);

    Task<IEnumerable<InstanceTrigger>> GetPermittedTriggersAsync();
}

public interface IInstanceLifecycleFsmFactory
{
    IInstanceLifecycleFsm GetOrCreate(string tenantId, string instanceId, string instanceName, InstanceLifecycleState initialState);
}
