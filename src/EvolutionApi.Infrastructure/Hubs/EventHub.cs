using Microsoft.AspNetCore.SignalR;

namespace EvolutionApi.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR hub for WhatsApp instance and messaging events.
/// Clients connect to /hubs/events and subscribe to instance channels.
/// </summary>
public sealed class EventHub : Hub<IWhatsAppEventsClient>
{
    public async Task JoinInstance(string instanceName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, instanceName);
    }

    public async Task LeaveInstance(string instanceName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, instanceName);
    }
}
