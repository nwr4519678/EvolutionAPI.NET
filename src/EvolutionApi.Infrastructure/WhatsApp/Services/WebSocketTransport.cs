using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using EvolutionApi.Domain.Interfaces;

namespace EvolutionApi.Infrastructure.WhatsApp.Services;

/// <summary>
/// WebSocket-based real-time event transport.
///
/// Clients connect to: GET /ws/{instanceName} with an <c>apikey</c> header.
/// After upgrading, they receive JSON events in the same format as HTTP webhooks.
///
/// Each EvolutionInstance can have multiple concurrent WebSocket subscribers.
/// Events are broadcast to all connected sockets for the instance.
///
/// Usage in Program.cs:
///   app.MapGet("/ws/{instanceName}", WebSocketTransport.HandleAsync);
///   builder.Services.AddSingleton&lt;WebSocketTransport&gt;();
/// </summary>
public sealed class WebSocketTransport(ILogger<WebSocketTransport> logger)
{
    // instance name → set of active connections
    private readonly ConcurrentDictionary<string, ConcurrentBag<WsConnection>> _connections =
        new(StringComparer.OrdinalIgnoreCase);

    // ── Connection management ─────────────────────────────────────────────────

    /// <summary>Registers a WebSocket connection for an instance and loops until disconnection.</summary>
    public async Task HandleConnectionAsync(string instanceName, WebSocket ws, CancellationToken ct)
    {
        var conn = new WsConnection(ws);
        var bag = _connections.GetOrAdd(instanceName, _ => new ConcurrentBag<WsConnection>());
        bag.Add(conn);

        logger.LogInformation("WebSocket client connected to instance {Instance}. Total: {Count}",
            instanceName, bag.Count);

        try
        {
            // Send a welcome frame
            await SendJsonAsync(ws, new { event_ = "connection", data = new { instanceName, status = "connected" } }, ct);

            // Receive loop — we don't process incoming messages, just drain to detect disconnect
            var buffer = new byte[256];
            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        { /* client disconnected */ }
        catch (Exception ex) { logger.LogWarning(ex, "WebSocket error for instance {Instance}", instanceName); }
        finally
        {
            conn.Dead = true;
            if (ws.State == WebSocketState.Open)
            {
                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing", CancellationToken.None); }
                catch { /* ignore */ }
            }
            logger.LogInformation("WebSocket client disconnected from instance {Instance}", instanceName);
        }
    }

    // ── Event broadcast ───────────────────────────────────────────────────────

    /// <summary>
    /// Broadcasts an event payload to all active WebSocket subscribers for an instance.
    /// Called by <see cref="WebhookDispatcher"/> after successfully dispatching HTTP webhooks.
    /// </summary>
    public async Task BroadcastAsync(string instanceName, string eventName, object data, CancellationToken ct)
    {
        if (!_connections.TryGetValue(instanceName, out var bag) || bag.IsEmpty) return;

        var envelope = new { @event = eventName, instance = instanceName, data };
        var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var payload = Encoding.UTF8.GetBytes(json);

        // Fan-out to all alive sockets (remove dead ones)
        var toRemove = new List<WsConnection>();
        foreach (var conn in bag)
        {
            if (conn.Dead || conn.Socket.State != WebSocketState.Open)
            {
                toRemove.Add(conn);
                continue;
            }
            try
            {
                await conn.Socket.SendAsync(new ArraySegment<byte>(payload),
                    WebSocketMessageType.Text, endOfMessage: true, ct);
            }
            catch
            {
                conn.Dead = true;
                toRemove.Add(conn);
            }
        }

        // Prune dead connections
        if (toRemove.Count > 0)
        {
            var remaining = bag.Where(c => !c.Dead).ToArray();
            _connections[instanceName] = new ConcurrentBag<WsConnection>(remaining);
        }
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    /// <summary>Returns the number of active connections per instance.</summary>
    public IReadOnlyDictionary<string, int> GetStats() =>
        _connections.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Count(c => !c.Dead && c.Socket.State == WebSocketState.Open),
            StringComparer.OrdinalIgnoreCase);

    // ── Private helpers ───────────────────────────────────────────────────────

    private static async Task SendJsonAsync(WebSocket ws, object obj, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private sealed class WsConnection(WebSocket socket)
    {
        public WebSocket Socket { get; } = socket;
        public bool Dead { get; set; }
    }
}

public static class WebSocketTransportExtensions
{
    public static Microsoft.AspNetCore.Builder.WebApplication MapWhatsAppWebSockets(
        this Microsoft.AspNetCore.Builder.WebApplication app,
        string pattern = "/ws/{instanceName}")
    {
        app.UseWebSockets();
        app.MapGet(pattern, async (string instanceName, Microsoft.AspNetCore.Http.HttpContext context,
            WebSocketTransport wsTransport, IApiKeyValidator apiKeyValidator) =>
        {
            var apiKey = context.Request.Headers["apikey"].FirstOrDefault();
            var validation = await apiKeyValidator.ValidateApiKeyAsync(
                new ApiKeyValidationRequest(apiKey, instanceName, context.Request.Path), context.RequestAborted);
            if (!validation.IsValid)
            {
                context.Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status401Unauthorized;
                return;
            }

            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await wsTransport.HandleConnectionAsync(instanceName, webSocket, context.RequestAborted);
            }
            else
            {
                context.Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest;
            }
        });
        return app;
    }
}

