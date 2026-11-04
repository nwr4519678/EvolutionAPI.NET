using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace EvolutionApi.Dashboard.Services;

/// <summary>
/// Manages the SignalR connection for real-time updates (session status, 
/// new messages, usage metrics, notifications).
/// </summary>
public sealed class RealtimeService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly NavigationManager _nav;

    // Events consumers can subscribe to
    public event Func<string, string, Task>? OnSessionStatusChanged; // (numberId, status)
    public event Func<ConversationMessageDto, Task>? OnNewMessage;
    public event Func<string, Task>? OnQrCodeUpdated; // (base64QR)
    public event Func<NotificationDto, Task>? OnNotification;
    public event Func<Task>? OnUsageUpdated;

    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public RealtimeService(NavigationManager nav)
    {
        _nav = nav;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_hub is not null) return;

        _hub = new HubConnectionBuilder()
            .WithUrl(_nav.ToAbsoluteUri("/hubs/dashboard"))
            .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)])
            .Build();

        _hub.On<string, string>("SessionStatusChanged", async (numberId, status) =>
        {
            if (OnSessionStatusChanged is not null)
                await OnSessionStatusChanged(numberId, status);
        });

        _hub.On<ConversationMessageDto>("NewMessage", async msg =>
        {
            if (OnNewMessage is not null)
                await OnNewMessage(msg);
        });

        _hub.On<string>("QrCodeUpdated", async qr =>
        {
            if (OnQrCodeUpdated is not null)
                await OnQrCodeUpdated(qr);
        });

        _hub.On<NotificationDto>("Notification", async notification =>
        {
            if (OnNotification is not null)
                await OnNotification(notification);
        });

        _hub.On("UsageUpdated", async () =>
        {
            if (OnUsageUpdated is not null)
                await OnUsageUpdated();
        });

        await _hub.StartAsync(ct);
    }

    public async Task SubscribeToNumberAsync(string numberId, CancellationToken ct = default)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("SubscribeToNumber", numberId, ct);
    }

    public async Task SubscribeToTenantAsync(string tenantId, CancellationToken ct = default)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("SubscribeToTenant", tenantId, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
            _hub = null;
        }
    }
}

