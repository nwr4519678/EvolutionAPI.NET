namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Real-time client notification contract (SignalR).
/// Broadcasts live instance events to connected WebSocket/SignalR clients isolated by TenantId and Instance.
/// </summary>
public interface IEventNotificationService
{
    Task NotifyInstanceEventAsync(
        string instanceName, string eventName, object data, CancellationToken ct = default);
    Task NotifyQrCodeAsync(
        string instanceName, string qrCodeBase64, CancellationToken ct = default);
    Task NotifyConnectionStatusAsync(
        string instanceName, string status, CancellationToken ct = default);
    Task NotifyMessageReceivedAsync(
        string instanceName, object message, CancellationToken ct = default);

    Task NotifyTenantEventAsync(
        string? tenantId, string instanceName, string eventName, object data, CancellationToken ct = default);
    Task NotifyTenantQrCodeAsync(
        string? tenantId, string instanceName, string qrCodeBase64, CancellationToken ct = default);
    Task NotifyTenantConnectionStatusAsync(
        string? tenantId, string instanceName, string status, CancellationToken ct = default);
    Task NotifyTenantMessageReceivedAsync(
        string? tenantId, string instanceName, object message, CancellationToken ct = default);
}
