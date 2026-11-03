using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR Hub acting as the real-time Agnostic Event Stream for the WhatsApp gateway.
/// Enforces tenant-isolated subscriber groups:
/// - Group "tenant_{tenantId}": receives all events across all instances owned by this tenant.
/// - Group "tenant_{tenantId}:{instanceName}": receives events isolated to a specific instance.
/// </summary>
public sealed class InstanceHub(ILogger<InstanceHub> logger) : Hub<IWhatsAppEventsClient>
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = ResolveTenantId();
        var tenantGroup = $"tenant_{tenantId}";

        await Groups.AddToGroupAsync(Context.ConnectionId, tenantGroup);
        logger.LogInformation("SignalR client {ConnectionId} connected to tenant event stream group '{TenantGroup}'",
            Context.ConnectionId, tenantGroup);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = ResolveTenantId();
        logger.LogInformation("SignalR client {ConnectionId} disconnected from tenant {TenantId}",
            Context.ConnectionId, tenantId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Subscribes the connection to a specific instance event stream within their tenant boundary.</summary>
    public async Task JoinInstance(string instanceName)
    {
        var tenantId = ResolveTenantId();
        var instanceGroup = $"tenant_{tenantId}:{instanceName}";

        await Groups.AddToGroupAsync(Context.ConnectionId, instanceGroup);
        logger.LogDebug("Client {ConnectionId} joined instance group '{InstanceGroup}'",
            Context.ConnectionId, instanceGroup);
    }

    /// <summary>Unsubscribes the connection from a specific instance event stream.</summary>
    public async Task LeaveInstance(string instanceName)
    {
        var tenantId = ResolveTenantId();
        var instanceGroup = $"tenant_{tenantId}:{instanceName}";

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, instanceGroup);
        logger.LogDebug("Client {ConnectionId} left instance group '{InstanceGroup}'",
            Context.ConnectionId, instanceGroup);
    }

    private string ResolveTenantId()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext is null) return "default";

        // 1. Query string: ?tenant=xyz or ?tenantId=xyz
        if (httpContext.Request.Query.TryGetValue("tenant", out var qTenant) && !string.IsNullOrWhiteSpace(qTenant))
            return qTenant.ToString();

        if (httpContext.Request.Query.TryGetValue("tenantId", out var qTenantId) && !string.IsNullOrWhiteSpace(qTenantId))
            return qTenantId.ToString();

        // 2. HTTP header: X-Tenant-Id
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var hTenant) && !string.IsNullOrWhiteSpace(hTenant))
            return hTenant.ToString();

        return "default";
    }
}
