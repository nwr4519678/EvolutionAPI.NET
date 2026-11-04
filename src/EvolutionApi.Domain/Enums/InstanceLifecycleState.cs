namespace EvolutionApi.Domain.Enums;

/// <summary>
/// Agnostic lifecycle states for a WhatsApp instance managed by the Stateless FSM.
/// </summary>
public enum InstanceLifecycleState
{
    Disconnected = 0,
    Initializing = 1,
    GeneratingQr = 2,
    Connecting = 3,
    Connected = 4,
    Retrying = 5,
    Suspended = 6
}
