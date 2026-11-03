using System.Text.Json;
using EvolutionApi.Domain.Interfaces;
using EvolutionApi.Infrastructure.WhatsApp.Services;

namespace EvolutionApi.Infrastructure.Services;

/// <summary>
/// Bridge that intercepts all engine webhook dispatches and broadcasts them over SignalR in real-time.
/// Ensures real-time push to frontend clients whenever WhatsApp events occur.
/// </summary>
public sealed class SignalRWebhookDispatcherBridge(
    WebhookDispatcher inner,
    IEventNotificationService notifications) : IWebhookDispatcher
{
    public async Task DispatchAsync(string instanceName, string eventName, JsonElement data, CancellationToken ct = default)
    {
        // 1. Dispatch via durable HTTP webhook pipeline
        await inner.DispatchAsync(instanceName, eventName, data, ct);

        // 2. Broadcast via SignalR real-time stream to connected clients
        _ = Task.Run(async () =>
        {
            try
            {
                await notifications.NotifyInstanceEventAsync(instanceName, eventName, data, CancellationToken.None);

                var normalized = eventName.ToUpperInvariant();
                if (normalized.Contains("QRCODE") && data.TryGetProperty("qrcode", out var qrObj) && qrObj.TryGetProperty("base64", out var b64))
                {
                    await notifications.NotifyQrCodeAsync(instanceName, b64.GetString() ?? string.Empty, CancellationToken.None);
                }
                else if (normalized.Contains("CONNECTION") && data.TryGetProperty("state", out var stateProp))
                {
                    await notifications.NotifyConnectionStatusAsync(instanceName, stateProp.GetString() ?? string.Empty, CancellationToken.None);
                }
                else if (normalized.Contains("MESSAGE"))
                {
                    await notifications.NotifyMessageReceivedAsync(instanceName, data, CancellationToken.None);
                }
            }
            catch
            {
                // Never allow background broadcast exceptions to bubble
            }
        }, CancellationToken.None);
    }
}
