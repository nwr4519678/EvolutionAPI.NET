using System.Text.Json;

namespace EvolutionApi.Dashboard.Models;

public sealed record DashboardInstance(
    string Id,
    string Name,
    string? Number,
    string ConnectionStatus,
    string Integration,
    DateTime CreatedAt);

public sealed class CreateInstanceRequest
{
    public string InstanceName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? Number { get; set; }
    public string? WebhookUrl { get; set; }
    public string[]? WebhookEvents { get; set; }
    public bool RejectCall { get; set; }
    public bool GroupsIgnore { get; set; }
    public bool AlwaysOnline { get; set; }
    public bool ReadMessages { get; set; }
}

public sealed record CreateInstanceResponse(string InstanceId, string InstanceName, string ApiKey, string Status);
public sealed record SendTextRequest(string Number, TextPayload Text);
public sealed record TextPayload(string Message);
public sealed record DashboardHealth(string Status, string? Service = null, DateTime? Timestamp = null);

public sealed record QrCodeResult(string? Code, string? Base64, string State, int? Count);

public sealed record ConnectResult(string? InstanceName, string? State, QrCodeResult? QrCode)
{
    public static ConnectResult FromJson(JsonElement root)
    {
        var instance = root.TryGetProperty("instance", out var i) ? i : default;
        var qr = root.TryGetProperty("qrcode", out var q) ? q : default;
        return new ConnectResult(
            instance.ValueKind == JsonValueKind.Object && instance.TryGetProperty("instanceName", out var n) ? n.GetString() : null,
            instance.ValueKind == JsonValueKind.Object && instance.TryGetProperty("state", out var s) ? s.GetString() : null,
            qr.ValueKind == JsonValueKind.Object
                ? new QrCodeResult(
                    qr.TryGetProperty("code", out var c) ? c.GetString() : null,
                    qr.TryGetProperty("base64", out var b) ? b.GetString() : null,
                    instance.TryGetProperty("state", out var state) ? state.GetString() ?? "close" : "close",
                    qr.TryGetProperty("count", out var count) && count.TryGetInt32(out var value) ? value : null)
                : null);
    }
}
