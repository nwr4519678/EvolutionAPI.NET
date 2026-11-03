namespace EvolutionApi.Domain.Enums;

/// <summary>
/// Finite State Machine triggers for instance lifecycle transitions.
/// </summary>
public enum InstanceTrigger
{
    Initialize = 0,
    QrGenerated = 1,
    CredentialsAccepted = 2,
    ConnectionLost = 3,
    DisconnectRequested = 4,
    FatalErrorOccurred = 5
}
