using Microsoft.AspNetCore.SignalR;

namespace EvolutionApi.Infrastructure.Hubs;

/// <summary>
/// Real-time SignalR hub for the WhatsApp AI Chatbot Management Dashboard.
/// Streamlined for telemetry, QR stream pushes, live inbox, and multi-tenant isolation.
/// </summary>
public sealed class DashboardHub : Hub
{
    public async Task SubscribeToNumber(string numberId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"number_{numberId}");
    }

    public async Task UnsubscribeFromNumber(string numberId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"number_{numberId}");
    }

    public async Task SubscribeToTenant(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }

    public async Task UnsubscribeFromTenant(string tenantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }
}

