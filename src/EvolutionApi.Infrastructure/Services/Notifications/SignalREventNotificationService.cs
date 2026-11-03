using EvolutionApi.Domain.Interfaces;
using EvolutionApi.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Infrastructure.Services.Notifications;

/// <summary>
/// Real-time agnostic event streamer implementing Domain IEventNotificationService.
/// Streams raw events, QR codes, connection states, and messages to SignalR client groups
/// strictly isolated by TenantId and Instance.
/// </summary>
public sealed class SignalREventNotificationService(
    IHubContext<InstanceHub, IWhatsAppEventsClient> instanceHubContext,
    IHubContext<EventHub, IWhatsAppEventsClient> eventHubContext,
    ILogger<SignalREventNotificationService> logger) : IEventNotificationService
{
    public async Task NotifyInstanceEventAsync(string instanceName, string eventName, object data, CancellationToken ct = default)
    {
        await NotifyTenantEventAsync("default", instanceName, eventName, data, ct);
    }

    public async Task NotifyQrCodeAsync(string instanceName, string qrCodeBase64, CancellationToken ct = default)
    {
        await NotifyTenantQrCodeAsync("default", instanceName, qrCodeBase64, ct);
    }

    public async Task NotifyConnectionStatusAsync(string instanceName, string status, CancellationToken ct = default)
    {
        await NotifyTenantConnectionStatusAsync("default", instanceName, status, ct);
    }

    public async Task NotifyMessageReceivedAsync(string instanceName, object message, CancellationToken ct = default)
    {
        await NotifyTenantMessageReceivedAsync("default", instanceName, message, ct);
    }

    public async Task NotifyTenantEventAsync(string? tenantId, string instanceName, string eventName, object data, CancellationToken ct = default)
    {
        var resolvedTenant = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId;
        var tenantGroup = $"tenant_{resolvedTenant}";
        var instanceGroup = $"tenant_{resolvedTenant}:{instanceName}";

        logger.LogDebug("[{TenantId}] SignalR streaming event '{Event}' for instance '{Instance}'",
            resolvedTenant, eventName, instanceName);

        // Stream to both instance-level and tenant-level subscriber groups
        var t1 = instanceHubContext.Clients.Group(tenantGroup).OnEvent(eventName, data);
        var t2 = instanceHubContext.Clients.Group(instanceGroup).OnEvent(eventName, data);
        var t3 = eventHubContext.Clients.Group(instanceName).OnEvent(eventName, data);

        await Task.WhenAll(t1, t2, t3);
    }

    public async Task NotifyTenantQrCodeAsync(string? tenantId, string instanceName, string qrCodeBase64, CancellationToken ct = default)
    {
        var resolvedTenant = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId;
        var tenantGroup = $"tenant_{resolvedTenant}";
        var instanceGroup = $"tenant_{resolvedTenant}:{instanceName}";

        var t1 = instanceHubContext.Clients.Group(tenantGroup).OnQrCode(instanceName, qrCodeBase64);
        var t2 = instanceHubContext.Clients.Group(instanceGroup).OnQrCode(instanceName, qrCodeBase64);
        var t3 = eventHubContext.Clients.Group(instanceName).OnQrCode(instanceName, qrCodeBase64);

        await Task.WhenAll(t1, t2, t3);
    }

    public async Task NotifyTenantConnectionStatusAsync(string? tenantId, string instanceName, string status, CancellationToken ct = default)
    {
        var resolvedTenant = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId;
        var tenantGroup = $"tenant_{resolvedTenant}";
        var instanceGroup = $"tenant_{resolvedTenant}:{instanceName}";

        var t1 = instanceHubContext.Clients.Group(tenantGroup).OnConnectionStatus(instanceName, status);
        var t2 = instanceHubContext.Clients.Group(instanceGroup).OnConnectionStatus(instanceName, status);
        var t3 = eventHubContext.Clients.Group(instanceName).OnConnectionStatus(instanceName, status);

        await Task.WhenAll(t1, t2, t3);
    }

    public async Task NotifyTenantMessageReceivedAsync(string? tenantId, string instanceName, object message, CancellationToken ct = default)
    {
        var resolvedTenant = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId;
        var tenantGroup = $"tenant_{resolvedTenant}";
        var instanceGroup = $"tenant_{resolvedTenant}:{instanceName}";

        var t1 = instanceHubContext.Clients.Group(tenantGroup).OnMessageReceived(instanceName, message);
        var t2 = instanceHubContext.Clients.Group(instanceGroup).OnMessageReceived(instanceName, message);
        var t3 = eventHubContext.Clients.Group(instanceName).OnMessageReceived(instanceName, message);

        await Task.WhenAll(t1, t2, t3);
    }
}
