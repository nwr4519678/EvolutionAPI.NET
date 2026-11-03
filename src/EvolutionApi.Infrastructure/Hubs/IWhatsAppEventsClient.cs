namespace EvolutionApi.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR client interface for WhatsApp real-time events.
/// Eliminates magic strings and provides compile-time type safety.
/// </summary>
public interface IWhatsAppEventsClient
{
    Task OnEvent(string eventName, object data);
    Task OnQrCode(string instanceName, string qrCode);
    Task OnConnectionStatus(string instanceName, string status);
    Task OnMessageReceived(string instanceName, object message);
}
