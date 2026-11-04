using System.Collections.Concurrent;
using System.Text.Json;
using Baileys;
using Baileys.Options;
using Baileys.Session;
using Baileys.Types;
using Baileys.Utils;
using EvolutionApi.Infrastructure.Persistence;
using EvolutionApi.Infrastructure.Security;
using EvolutionApi.Infrastructure.WhatsApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QRCoder;

namespace EvolutionApi.Infrastructure.WhatsApp.Engine;

/// <summary>
/// Production WhatsApp engine: one <see cref="BaileysClient"/> per Evolution instance.
/// Credentials + all Signal keys are persisted encrypted in SQL Server.
/// All inbound events are forwarded to webhooks and persisted to the local history store.
/// </summary>
public sealed class BaileysNetWhatsAppEngine(
    IServiceScopeFactory scopes,
    SessionKeyStore sessionStore,
    ISecretProtector protector,
    IWebhookDispatcher webhooks,
    IConfiguration configuration,
    WaMediaUploader mediaUploader,
    IHttpClientFactory httpFactory,
    ILoggerFactory loggerFactory,
    WaInboundMessageStore inboundStore,
    WaRawFrameInterceptor frameInterceptor,
    OutboundUrlValidator outboundUrls,
    ILogger<BaileysNetWhatsAppEngine> logger) : IWhatsAppEngine
{
    private readonly ConcurrentDictionary<string, ClientSession> _sessions = new(StringComparer.Ordinal);
    private readonly IHttpClientFactory _httpFactory = httpFactory;   // for CDN/media re-download
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    public Task StartAsync(string instanceId, string instanceName, string? number, CancellationToken ct)
    {
        var instance = new Instance
        {
            Id = instanceId,
            Name = instanceName,
            Number = number,
            Integration = "WHATSAPP-BAILEYS"
        };
        return StartAsync(instance, ct);
    }

    public Task StartAsync(Instance instance, CancellationToken ct)
    {
        _sessions.GetOrAdd(instance.Id, _ => CreateSession(instance));
        StartConnection(_sessions[instance.Id]);
        return Task.CompletedTask;
    }

    public async Task StopAsync(string instanceId, CancellationToken ct)
    {
        if (!_sessions.TryRemove(instanceId, out var session)) return;
        session.Lifecycle.Cancel();
        try { await session.Client.DisposeAsync(); }
        finally { await SaveConnectionStateAsync(instanceId, "close", CancellationToken.None); }
    }

    public async Task<JsonElement> ConnectAsync(string instanceId, string? number, CancellationToken ct)
    {
        var instance = await FindInstanceAsync(instanceId, ct);
        var session = _sessions.GetOrAdd(instance.Id, _ => CreateSession(instance, number));
        StartConnection(session);

        if (session.State != "open" && session.Qr is null)
        {
            try { await session.QrAvailable.Task.WaitAsync(TimeSpan.FromSeconds(25), ct); }
            catch (TimeoutException) { }
        }

        if (session.Qr is { } qr)
            return JsonSerializer.SerializeToElement(new
            {
                instance = new { instanceName = instance.Name, state = session.State },
                qrcode = new { code = qr, base64 = session.QrBase64, count = session.QrCount }
            }, JsonOpts);

        return JsonSerializer.SerializeToElement(new
        {
            instance = new { instanceName = instance.Name, state = session.State }
        }, JsonOpts);
    }

    public async Task<string> GetStateAsync(string instanceId, CancellationToken ct)
    {
        if (_sessions.TryGetValue(instanceId, out var session)) return session.State;
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Instances.AsNoTracking()
                   .Where(x => x.Id == instanceId).Select(x => x.ConnectionStatus)
                   .SingleOrDefaultAsync(ct) ?? "close";
    }

    // ── Presence ──────────────────────────────────────────────────────────────

    public async Task<JsonElement> SetPresenceAsync(string instanceId, string presenceType, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        await socket.SetPresenceAsync(presenceType, ct);
        return JsonSerializer.SerializeToElement(new { presence = presenceType }, JsonOpts);
    }

    public async Task<JsonElement> SendChatStateAsync(string instanceId, string jid, string state, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        await socket.SendChatStateAsync(jid, state, ct);
        return JsonSerializer.SerializeToElement(new { jid, state }, JsonOpts);
    }

    // ── Messages ───────────────────────────────────────────────────────────────

    public async Task<JsonElement> SendAsync(string instanceId, string operation, JsonElement payload, CancellationToken ct)
    {
        if (!_sessions.TryGetValue(instanceId, out var session))
            throw new InvalidOperationException($"Instance '{instanceId}' is not running.");
        if (session.State != "open")
            throw new InvalidOperationException($"Instance '{instanceId}' is not connected (state: {session.State}).");

        var socket = new WaSocket(session.Client);
        var signalStore = session.SignalKeys;
        var encryptor = new WaMessageEncryptor(signalStore, sessionStore,
            loggerFactory.CreateLogger<WaMessageEncryptor>());
        var sender = new WaMessageSender(encryptor, mediaUploader,
            loggerFactory.CreateLogger<WaMessageSender>());

        return operation switch
        {
            "sendText" => await DispatchSendText(instanceId, socket, sender, payload, ct),
            "sendMedia" => await DispatchSendMedia(instanceId, socket, sender, payload, ct),
            "sendWhatsAppAudio" or "sendPtv" => await DispatchSendAudio(instanceId, socket, sender, payload, ct),
            "sendSticker" => await DispatchSendMedia(instanceId, socket, sender, payload, ct, "stickerMessage"),
            "sendLocation" => await DispatchSendLocation(instanceId, socket, sender, payload, ct),
            "sendContact" => await DispatchSendContact(instanceId, socket, sender, payload, ct),
            "sendReaction" => await DispatchSendReaction(instanceId, socket, sender, payload, ct),
            "sendPoll" => await DispatchSendPoll(instanceId, socket, sender, payload, ct),
            "sendList" => await DispatchSendList(instanceId, socket, sender, payload, ct),
            "sendStatus" => await sender.SendStatusAsync(instanceId, socket, "text",
                payload.TryGetProperty("content", out var sc) ? sc.GetString() ?? "" : "", ct),
            "sendButtons" or "sendTemplate" => throw new WhatsAppEngineUnavailableException(
                $"'{operation}' requires interactive button templates not yet supported by the WA Business API via this library."),
            _ => throw new ArgumentException($"Unknown send operation: {operation}")
        };
    }

    // ── Send dispatchers ────────────────────────────────────────────────────────

    private static string Str(JsonElement el, string key) =>
        el.TryGetProperty(key, out var v) ? v.GetString() ?? "" : "";
    private static string ToJid(string number) =>
        number.Contains('@') ? number : $"{new string(number.Where(char.IsDigit).ToArray())}@s.whatsapp.net";

    private async Task<JsonElement> DispatchSendText(string instanceId, WaSocket socket, WaMessageSender sender, JsonElement p, CancellationToken ct)
    {
        var toJid = ToJid(Str(p, "number"));
        var text = p.TryGetProperty("text", out var t) && t.TryGetProperty("message", out var m) ? m.GetString() ?? "" : Str(p, "message");
        var quotedId = p.TryGetProperty("quoted", out var q) && q.TryGetProperty("key", out var k) ? k.GetString() : null;
        var quotedParticipant = p.TryGetProperty("quoted", out var q2) && q2.TryGetProperty("participant", out var qp) ? qp.GetString() : null;
        string[]? mentioned = p.TryGetProperty("mentioned", out var mj) && mj.ValueKind == JsonValueKind.Array
            ? mj.EnumerateArray().Select(x => x.GetString() ?? "").ToArray() : null;
        return await sender.SendTextAsync(instanceId, socket, toJid, text, quotedId, quotedParticipant, mentioned, ct);
    }

    private async Task<JsonElement> DispatchSendMedia(string instanceId, WaSocket socket, WaMessageSender sender,
        JsonElement p, CancellationToken ct, string? forcedType = null)
    {
        var toJid = ToJid(Str(p, "number"));
        var caption = Str(p, "caption");
        var fileName = Str(p, "fileName");
        var mimeType = Str(p, "mimeType") is "" ? "application/octet-stream" : Str(p, "mimeType");
        var messageType = forcedType ?? (Str(p, "mediatype") is "" ? "imageMessage" : $"{Str(p, "mediatype")}Message");

        // Resolve bytes from base64 or URL
        byte[] mediaBytes;
        if (p.TryGetProperty("mediaBase64", out var b64) && b64.GetString() is { Length: > 0 } encoded)
            mediaBytes = Convert.FromBase64String(encoded);
        else if (p.TryGetProperty("media", out var m) && m.GetString() is { Length: > 0 } mediaStr)
        {
            if (mediaStr.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                mediaBytes = await DownloadExternalMediaAsync(mediaStr, ct);
            }
            else
                mediaBytes = Convert.FromBase64String(mediaStr);
        }
        else throw new ArgumentException("media or mediaBase64 is required for media messages");

        return await sender.SendMediaAsync(instanceId, socket, toJid, messageType, mediaBytes,
            mimeType, caption is "" ? null : caption, fileName is "" ? null : fileName, ct);
    }

    private async Task<JsonElement> DispatchSendAudio(string instanceId, WaSocket socket, WaMessageSender sender, JsonElement p, CancellationToken ct)
    {
        var toJid = ToJid(Str(p, "number"));
        var audio = Str(p, "audio");
        var mimeType = Str(p, "mimetype") is "" ? "audio/ogg; codecs=opus" : Str(p, "mimetype");
        var bytes = audio.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? await DownloadExternalMediaAsync(audio, ct)
            : Convert.FromBase64String(audio);
        return await sender.SendWhatsAppAudioAsync(instanceId, socket, toJid, bytes, mimeType, ct);
    }

    private async Task<byte[]> DownloadExternalMediaAsync(string url, CancellationToken ct)
    {
        if (!await outboundUrls.IsAllowedAsync(url, ct))
            throw new ArgumentException("The media URL is not an allowed public HTTP(S) destination.");

        const int defaultMaxBytes = 32 * 1024 * 1024;
        var maxBytes = int.TryParse(configuration["Media:MaxDownloadBytes"], out var configured)
            ? Math.Clamp(configured, 1, 100 * 1024 * 1024)
            : defaultMaxBytes;
        using var response = await _httpFactory.CreateClient("media").GetAsync(url,
            HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is long contentLength && contentLength > maxBytes)
            throw new ArgumentException($"The media download exceeds the {maxBytes} byte limit.");

        await using var input = await response.Content.ReadAsStreamAsync(ct);
        await using var output = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0)
        {
            if (output.Length + read > maxBytes)
                throw new ArgumentException($"The media download exceeds the {maxBytes} byte limit.");
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        return output.ToArray();
    }

    private async Task<JsonElement> DispatchSendLocation(string instanceId, WaSocket socket, WaMessageSender sender, JsonElement p, CancellationToken ct)
    {
        var toJid = ToJid(Str(p, "number"));
        var lat = p.TryGetProperty("latitude", out var la) ? la.GetDouble() : 0;
        var lon = p.TryGetProperty("longitude", out var lo) ? lo.GetDouble() : 0;
        return await sender.SendLocationAsync(instanceId, socket, toJid, lat, lon,
            Str(p, "name") is "" ? null : Str(p, "name"),
            Str(p, "address") is "" ? null : Str(p, "address"), ct);
    }

    private async Task<JsonElement> DispatchSendContact(string instanceId, WaSocket socket, WaMessageSender sender, JsonElement p, CancellationToken ct)
    {
        var toJid = ToJid(Str(p, "number"));
        // Build vCard from first contact in the array
        var contact = p.TryGetProperty("contact", out var ca) && ca.ValueKind == JsonValueKind.Array
            ? ca.EnumerateArray().FirstOrDefault() : default;
        var name = contact.ValueKind != JsonValueKind.Undefined ? Str(contact, "fullName") : "";
        var phone = contact.ValueKind != JsonValueKind.Undefined ? Str(contact, "waNumber") : "";
        var vCard = $"BEGIN:VCARD\nVERSION:3.0\nFN:{name}\nTEL;type=CELL;type=VOICE;waid={phone}:+{phone}\nEND:VCARD";
        return await sender.SendContactAsync(instanceId, socket, toJid, name, vCard, ct);
    }

    private async Task<JsonElement> DispatchSendReaction(string instanceId, WaSocket socket, WaMessageSender sender, JsonElement p, CancellationToken ct)
    {
        var toJid = ToJid(Str(p, "number"));
        var reaction = p.TryGetProperty("reaction", out var r) ? r : default;
        var key = reaction.ValueKind != JsonValueKind.Undefined ? Str(reaction, "key") : "";
        var emoji = reaction.ValueKind != JsonValueKind.Undefined ? Str(reaction, "reaction") : "";
        return await sender.SendReactionAsync(instanceId, socket, toJid, key, false, emoji, ct);
    }

    private async Task<JsonElement> DispatchSendPoll(string instanceId, WaSocket socket, WaMessageSender sender, JsonElement p, CancellationToken ct)
    {
        var toJid = ToJid(Str(p, "number"));
        var poll = p.TryGetProperty("pollMessage", out var pm) ? pm : default;
        var name = poll.ValueKind != JsonValueKind.Undefined ? Str(poll, "name") : "Poll";
        var options = poll.ValueKind != JsonValueKind.Undefined && poll.TryGetProperty("values", out var vs)
            ? vs.EnumerateArray().Select(v => v.GetString() ?? "").ToArray() : [];
        var selectable = poll.ValueKind != JsonValueKind.Undefined && poll.TryGetProperty("selectableCount", out var sc)
            ? sc.GetInt32() : 1;
        return await sender.SendPollAsync(instanceId, socket, toJid, name, options, selectable, ct);
    }

    private async Task<JsonElement> DispatchSendList(string instanceId, WaSocket socket, WaMessageSender sender, JsonElement p, CancellationToken ct)
    {
        var toJid = ToJid(Str(p, "number"));
        var sections = p.TryGetProperty("sections", out var secs) && secs.ValueKind == JsonValueKind.Array
            ? secs.EnumerateArray().Select(sec =>
            {
                var rows = sec.TryGetProperty("rows", out var rs) && rs.ValueKind == JsonValueKind.Array
                    ? rs.EnumerateArray().Select(r => new ListRow(Str(r, "title"), Str(r, "rowId"), Str(r, "description") is "" ? null : Str(r, "description"))).ToArray()
                    : Array.Empty<ListRow>();
                return new ListSection(Str(sec, "title") is "" ? null : Str(sec, "title"), rows);
            }).ToArray()
            : Array.Empty<ListSection>();
        return await sender.SendListAsync(instanceId, socket, toJid,
            Str(p, "title"), Str(p, "description"), Str(p, "buttonText"),
            sections, Str(p, "footer") is "" ? null : Str(p, "footer"), ct);
    }

    // ── Chat operations ────────────────────────────────────────────────────────

    public async Task<JsonElement> CheckWhatsAppNumbersAsync(string instanceId, string[] numbers, CancellationToken ct)
        => await GetSocket(instanceId).CheckWhatsAppNumbersAsync(numbers, ct);

    public async Task<JsonElement> MarkMessagesAsReadAsync(string instanceId, string jid, string[] ids, string? participant, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        await socket.MarkAsReadAsync(jid, ids, participant, ct);
        await PersistMarkReadAsync(instanceId, jid, ct);
        return JsonSerializer.SerializeToElement(new { jid, count = ids.Length }, JsonOpts);
    }

    public async Task DeleteMessageForEveryoneAsync(string instanceId, string jid, string msgId, bool fromMe, string? participant, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        await socket.DeleteMessageForEveryoneAsync(jid, msgId, fromMe, participant, ct);
        await PersistDeleteMessageAsync(instanceId, new[] { msgId }, jid, ct);
    }

    public async Task<JsonElement> FetchProfilePictureUrlAsync(string instanceId, string jid, CancellationToken ct)
    {
        var url = await GetSocket(instanceId).FetchProfilePictureUrlAsync(jid, ct);
        return JsonSerializer.SerializeToElement(new { jid, profilePictureUrl = url }, JsonOpts);
    }

    public async Task<JsonElement> FetchPrivacySettingsAsync(string instanceId, CancellationToken ct)
        => await GetSocket(instanceId).FetchPrivacySettingsAsync(ct);

    public async Task UpdatePrivacySettingsAsync(string instanceId, Dictionary<string, string> categories, CancellationToken ct)
        => await GetSocket(instanceId).UpdatePrivacySettingsAsync(categories, ct);

    public async Task UpdateProfileNameAsync(string instanceId, string name, CancellationToken ct)
    {
        await GetSocket(instanceId).UpdateProfileNameAsync(name, ct);
        await PersistProfileNameAsync(instanceId, name, ct);
    }

    public async Task UpdateProfileStatusAsync(string instanceId, string status, CancellationToken ct)
        => await GetSocket(instanceId).UpdateProfileStatusAsync(status, ct);

    public async Task UpdateBlockStatusAsync(string instanceId, string jid, string action, CancellationToken ct)
        => await GetSocket(instanceId).UpdateBlockStatusAsync(jid, action, ct);

    // ── Groups ────────────────────────────────────────────────────────────────

    public Task<JsonElement> CreateGroupAsync(string instanceId, string subject, string[] participants, CancellationToken ct)
        => GetSocket(instanceId).CreateGroupAsync(subject, participants, ct);

    public Task<JsonElement> FetchGroupMetadataAsync(string instanceId, string groupJid, CancellationToken ct)
        => GetSocket(instanceId).FetchGroupMetadataAsync(groupJid, ct);

    public Task<JsonElement> FetchAllGroupsAsync(string instanceId, CancellationToken ct)
        => GetSocket(instanceId).FetchAllGroupsAsync(ct);

    public Task<JsonElement> UpdateGroupParticipantsAsync(string instanceId, string groupJid, string[] participants, string action, CancellationToken ct)
        => GetSocket(instanceId).UpdateParticipantsAsync(groupJid, participants, action, ct);

    public async Task UpdateGroupSubjectAsync(string instanceId, string groupJid, string subject, CancellationToken ct)
        => await GetSocket(instanceId).UpdateGroupSubjectAsync(groupJid, subject, ct);

    public async Task UpdateGroupDescriptionAsync(string instanceId, string groupJid, string description, CancellationToken ct)
        => await GetSocket(instanceId).UpdateGroupDescriptionAsync(groupJid, description, ct);

    public async Task<JsonElement> FetchGroupInviteCodeAsync(string instanceId, string groupJid, CancellationToken ct)
    {
        var code = await GetSocket(instanceId).FetchGroupInviteCodeAsync(groupJid, ct);
        return JsonSerializer.SerializeToElement(new { inviteCode = code, inviteUrl = $"https://chat.whatsapp.com/{code}" }, JsonOpts);
    }

    public async Task<JsonElement> RevokeGroupInviteCodeAsync(string instanceId, string groupJid, CancellationToken ct)
    {
        var code = await GetSocket(instanceId).RevokeGroupInviteCodeAsync(groupJid, ct);
        return JsonSerializer.SerializeToElement(new { inviteCode = code, inviteUrl = $"https://chat.whatsapp.com/{code}" }, JsonOpts);
    }

    public Task<JsonElement> FetchGroupInviteInfoAsync(string instanceId, string code, CancellationToken ct)
        => GetSocket(instanceId).FetchGroupInviteInfoAsync(code, ct);

    public Task<JsonElement> AcceptGroupInviteAsync(string instanceId, string code, CancellationToken ct)
        => GetSocket(instanceId).AcceptGroupInviteAsync(code, ct);

    public async Task UpdateGroupSettingAsync(string instanceId, string groupJid, string setting, bool enable, CancellationToken ct)
        => await GetSocket(instanceId).UpdateGroupSettingAsync(groupJid, setting, enable, ct);

    public async Task ToggleEphemeralAsync(string instanceId, string groupJid, int durationSeconds, CancellationToken ct)
        => await GetSocket(instanceId).ToggleEphemeralAsync(groupJid, durationSeconds, ct);

    public async Task LeaveGroupAsync(string instanceId, string groupJid, CancellationToken ct)
        => await GetSocket(instanceId).LeaveGroupAsync(groupJid, ct);

    public async Task<JsonElement> UpdateGroupPictureAsync(string instanceId, string groupJid, byte[] jpegBytes, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        return await socket.UpdateProfilePictureAsync(groupJid, jpegBytes, ct);
    }

    /// <summary>Sends each JID the group invite link as a text message.</summary>
    public async Task<JsonElement> SendGroupInviteAsync(string instanceId, string groupJid, string[] numbers, CancellationToken ct)
    {
        var inviteCodeResult = await FetchGroupInviteCodeAsync(instanceId, groupJid, ct);
        var link = inviteCodeResult.TryGetProperty("inviteUrl", out var u) ? u.GetString() : $"https://chat.whatsapp.com/unknown";
        var results = new List<object>();
        foreach (var number in numbers)
        {
            try
            {
                var r = await SendAsync(instanceId, "sendText",
                    JsonSerializer.SerializeToElement(new
                    {
                        number, text = new { message = link }
                    }, JsonOpts), ct);
                results.Add(new { number, sent = true });
            }
            catch (Exception ex)
            {
                results.Add(new { number, sent = false, error = ex.Message });
            }
        }
        return JsonSerializer.SerializeToElement(new { groupJid, inviteUrl = link, sent = results }, JsonOpts);
    }

    // ── Profile picture ───────────────────────────────────────────────────────

    public async Task<JsonElement> UpdateProfilePictureAsync(string instanceId, string jid, byte[] jpegBytes, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        // Resize/normalize to JPEG 640×640 if needed — passed in as-is (caller ensures JPEG)
        return await socket.UpdateProfilePictureAsync(jid, jpegBytes, ct);
    }

    public Task<JsonElement> RemoveProfilePictureAsync(string instanceId, string jid, CancellationToken ct)
        => GetSocket(instanceId).RemoveProfilePictureAsync(jid, ct);

    // ── Profile / business ─────────────────────────────────────────────────────

    public async Task<JsonElement> FetchProfileAsync(string instanceId, string jid, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        var picUrl = await socket.FetchProfilePictureUrlAsync(jid, ct);
        var status = await socket.FetchProfileStatusAsync(jid, ct);
        return JsonSerializer.SerializeToElement(new { jid, profilePictureUrl = picUrl, status }, JsonOpts);
    }

    public Task<JsonElement> FetchBusinessProfileAsync(string instanceId, string jid, CancellationToken ct)
        => GetSocket(instanceId).FetchBusinessProfileAsync(jid, ct);

    // ── Calls (JINGLE protocol) ────────────────────────────────────────────────

    /// <summary>
    /// Initiates a WhatsApp voice/video call using the JINGLE signalling protocol.
    /// Sends a w:call IQ with an offer node containing SDP-encoded media capabilities.
    /// </summary>
    public async Task<JsonElement> OfferCallAsync(string instanceId, JsonElement payload, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        var toJid = payload.TryGetProperty("number", out var n) ? $"{new string((n.GetString() ?? "").Where(char.IsDigit).ToArray())}@s.whatsapp.net" : "";
        var callType = payload.TryGetProperty("type", out var t) ? t.GetString() ?? "voice" : "voice";
        var callId = System.Security.Cryptography.RandomNumberGenerator.GetHexString(16);

        // Build JINGLE offer node — standard WA call initiation
        var offerNode = new BinaryNode
        {
            Tag = "iq",
            Attrs = new Dictionary<string, string>
            {
                ["to"] = toJid, ["type"] = "set",
                ["id"] = callId, ["xmlns"] = "w:call"
            },
            Content = new BinaryNodeList([
                new BinaryNode
                {
                    Tag = "offer",
                    Attrs = new Dictionary<string, string>
                    {
                        ["call-id"] = callId,
                        ["call-creator"] = await GetMyJidFromSessionAsync(instanceId, ct)
                    },
                    Content = new BinaryNodeList(BuildCallMediaNodes(callType))
                }
            ])
        };

        await socket.SendNodeAsync(offerNode, ct);
        return JsonSerializer.SerializeToElement(new
        {
            callId, toJid, type = callType, status = "offer_sent"
        }, JsonOpts);
    }

    private static BinaryNode[] BuildCallMediaNodes(string callType)
    {
        var nodes = new List<BinaryNode>
        {
            new() { Tag = "audio", Attrs = new() { ["enc"] = "zrtp", ["dec"] = "zrtp" }, Content = null }
        };
        if (callType == "video")
            nodes.Add(new() { Tag = "video", Attrs = new() { ["enc"] = "zrtp", ["dec"] = "zrtp" }, Content = null });
        return nodes.ToArray();
    }

    private async Task<string> GetMyJidFromSessionAsync(string instanceId, CancellationToken ct)
    {
        var json = await sessionStore.ReadAsync(instanceId, "baileys-net.credentials", ct);
        if (json is null) return "unknown@s.whatsapp.net";
        try
        {
            var creds = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            return creds.TryGetProperty("me", out var me) && me.TryGetProperty("id", out var id)
                ? id.GetString() ?? "unknown@s.whatsapp.net" : "unknown@s.whatsapp.net";
        }
        catch { return "unknown@s.whatsapp.net"; }
    }

    // ── Labels ────────────────────────────────────────────────────────────────

    /// <summary>Fetches all labels from the WA account (app-state sync).</summary>
    public async Task<JsonElement> FetchLabelsAsync(string instanceId, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        try
        {
            var result = await socket.QueryAsync(new BinaryNode
            {
                Tag = "iq",
                Attrs = new Dictionary<string, string>
                    { ["to"] = "s.whatsapp.net", ["type"] = "get", ["xmlns"] = "w:label:user" },
                Content = new BinaryNodeList([
                    new BinaryNode { Tag = "labels", Attrs = [], Content = null }
                ])
            }, ct);

            var labelsNode = BinaryNodeExtensions.GetChild(result, "labels");
            var labels = labelsNode is null
                ? Array.Empty<object>()
                : BinaryNodeExtensions.GetChildren(labelsNode)
                    .Select(l => (object)new
                    {
                        id = l.Attrs.TryGetValue("id", out var lid) ? lid : null,
                        name = l.Attrs.TryGetValue("name", out var lname) ? lname : null,
                        color = l.Attrs.TryGetValue("color", out var lcolor) ? lcolor : null,
                        predefinedId = l.Attrs.TryGetValue("predefined_id", out var lpid) ? lpid : null
                    }).ToArray();

            return JsonSerializer.SerializeToElement(new { labels }, JsonOpts);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch labels for instance {Id}", instanceId);
            return JsonSerializer.SerializeToElement(new { labels = Array.Empty<object>() }, JsonOpts);
        }
    }

    /// <summary>Adds or removes a label from a chat.</summary>
    public async Task<JsonElement> HandleLabelAsync(string instanceId, string label, string chatId, string action, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        try
        {
            var verb = action == "add" ? "add" : "remove";
            await socket.SendNodeAsync(new BinaryNode
            {
                Tag = "iq",
                Attrs = new Dictionary<string, string>
                    { ["to"] = "s.whatsapp.net", ["type"] = "set", ["xmlns"] = "w:label:user" },
                Content = new BinaryNodeList([
                    new BinaryNode
                    {
                        Tag = "label_jid",
                        Attrs = new Dictionary<string, string>
                        {
                            ["jid"] = chatId, ["label_id"] = label, ["type"] = verb
                        },
                        Content = null
                    }
                ])
            }, ct);
            return JsonSerializer.SerializeToElement(new { label, chatId, action = verb, success = true }, JsonOpts);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to {Action} label {Label} for chat {Chat}", action, label, chatId);
            throw new InvalidOperationException($"Label operation failed: {ex.Message}", ex);
        }
    }

    /// <summary>Creates a new label on the WA account.</summary>
    public async Task<JsonElement> CreateLabelAsync(string instanceId, string name, string color, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        return await socket.CreateLabelAsync(name, color, ct);
    }

    /// <summary>Updates an existing label's name and/or color.</summary>
    public async Task<JsonElement> UpdateLabelAsync(string instanceId, string labelId, string? name, string? color, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        return await socket.UpdateLabelAsync(labelId, name, color, ct);
    }

    /// <summary>Permanently deletes a label from the WA account.</summary>
    public async Task<JsonElement> DeleteLabelAsync(string instanceId, string labelId, CancellationToken ct)
    {
        var socket = GetSocket(instanceId);
        return await socket.DeleteLabelAsync(labelId, ct);
    }

    // ── Session construction ───────────────────────────────────────────────────

    private ClientSession CreateSession(Instance instance, string? number = null)
    {
        var options = new BaileysOptions
        {
            PhoneNumber = number ?? instance.Number ?? configuration["Evolution:PhoneNumber"] ?? string.Empty,
            PrintQrInTerminal = false
        };
        var authProvider = new SqlAuthStateProvider(instance.Id, sessionStore);
        var signalKeys = new SqlSignalKeyStore(instance.Id, scopes, protector);
        var ev = new BaileysEventEmitter();
        var client = new BaileysClient(authProvider, ev, Options.Create(options), new NullLogger());
        var session = new ClientSession(instance, client, authProvider, signalKeys, ev);

        ev.On<ConnectionUpdateEvent>("connection.update", update => OnConnectionUpdate(session, update));
        ev.On<MessagesUpsertEvent>("messages.upsert", e => OnMessagesUpsert(session, e));
        ev.On<MessagesDeleteEvent>("messages.delete", e => OnMessagesDelete(session, e));
        ev.On<PresenceUpdateEvent>("presence.update", e => OnPresenceUpdate(session, e));
        ev.On<GroupParticipantsUpdateEvent>("group-participants.update", e => OnGroupParticipantsUpdate(session, e));
        ev.On<WaCallEvent[]>("call", calls => OnCall(session, calls));

        // Wire inbound decryptor — intercepts raw "message" binary nodes before the library
        // converts them to MinimalMessage, enabling full E2E decryption of inbound content.
        var decryptor = new WaInboundDecryptor(signalKeys, sessionStore);
        frameInterceptor.TryRegister(client, instance.Id, decryptor, inboundStore);

        return session;
    }

    private void StartConnection(ClientSession session)
    {
        if (Interlocked.Exchange(ref session.ConnectionStarted, 1) != 0) return;
        _ = RunConnectionAsync(session);
    }

    private async Task RunConnectionAsync(ClientSession session)
    {
        try { await session.Client.ConnectAsync(session.Lifecycle.Token); }
        catch (OperationCanceledException) when (session.Lifecycle.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Baileys.NET connection failed for {Instance}", session.Instance.Name);
            session.State = "close";
            await SaveConnectionStateAsync(session.Instance.Id, "close", CancellationToken.None);
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnConnectionUpdate(ClientSession session, ConnectionUpdateEvent update)
    {
        if (!string.IsNullOrWhiteSpace(update.Qr))
        {
            session.Qr = update.Qr;
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(update.Qr, QRCodeGenerator.ECCLevel.Q);
            session.QrBase64 = "data:image/png;base64," + Convert.ToBase64String(new PngByteQRCode(data).GetGraphic(20));
            session.QrCount++;
            session.State = "connecting";
            session.QrAvailable.TrySetResult(true);
            QueueWebhook(session.Instance.Name, "QRCODE_UPDATED", new
            {
                instance = session.Instance.Name,
                qrcode = new { code = session.Qr, base64 = session.QrBase64, count = session.QrCount }
            });
        }

        if (update.Connection is { } state)
        {
            session.State = state.ToString().ToLowerInvariant();
            if (state == WaConnectionState.Open) { session.Qr = null; session.QrBase64 = null; }
            _ = SaveConnectionStateAsync(session.Instance.Id, session.State, CancellationToken.None);
            QueueWebhook(session.Instance.Name, "CONNECTION_UPDATE", new
            {
                instance = session.Instance.Name,
                state = session.State,
                lastDisconnect = update.LastDisconnect is { } ld
                    ? (object)new { error = ld.Error, date = ld.Date } : null
            });
        }
    }

    private void OnMessagesUpsert(ClientSession session, MessagesUpsertEvent e)
    {
        var msgsJson = JsonSerializer.SerializeToElement(e.Messages, JsonOpts);

        // Enrich each MinimalMessage with decrypted content if available
        var enriched = e.Messages.Select(msg =>
        {
            var msgId = msg.Key?.Id ?? "";
            var content = !string.IsNullOrEmpty(msgId) ? inboundStore.TakeContent(msgId) : null;
            return (object)new
            {
                key = msg.Key,
                messageTimestamp = msg.MessageTimestamp,
                // decryptedContent is non-null only if WaRawFrameInterceptor successfully
                // intercepted and decrypted the raw Signal payload for this message.
                decryptedContent = content is not null
                    ? JsonSerializer.Deserialize<object>(content, JsonOpts)
                    : null
            };
        }).ToArray();

        QueueWebhook(session.Instance.Name, "MESSAGES_UPSERT", new
        {
            instance = session.Instance.Name,
            data = enriched,
            type = e.Type.ToString().ToLowerInvariant()
        });

        // Persist each message to local store
        foreach (var msg in msgsJson.EnumerateArray())
            _ = PersistMessageAsync(session.Instance.Id, msg);
    }

    private void OnMessagesDelete(ClientSession session, MessagesDeleteEvent e)
    {
        QueueWebhook(session.Instance.Name, "MESSAGES_DELETE", new
        {
            instance = session.Instance.Name,
            data = new { keys = e.Keys, jid = e.Jid, all = e.All }
        });
        if (e.Keys is not null)
        {
            var ids = e.Keys.Select(k => k.Id).Where(id => id is not null).Cast<string>().ToArray();
            if (ids.Length > 0)
                _ = PersistDeleteMessageAsync(session.Instance.Id, ids, e.Jid, CancellationToken.None);
        }
    }

    private void OnPresenceUpdate(ClientSession session, PresenceUpdateEvent e)
    {
        QueueWebhook(session.Instance.Name, "PRESENCE_UPDATE", new
        {
            instance = session.Instance.Name,
            data = new { id = e.Id, presences = e.Presences }
        });
    }

    private void OnGroupParticipantsUpdate(ClientSession session, GroupParticipantsUpdateEvent e)
    {
        QueueWebhook(session.Instance.Name, "GROUP_PARTICIPANTS_UPDATE", new
        {
            instance = session.Instance.Name,
            data = new { id = e.Id, author = e.Author, participants = e.Participants, action = e.Action.ToString().ToLowerInvariant() }
        });
    }

    private void OnCall(ClientSession session, WaCallEvent[] calls)
    {
        QueueWebhook(session.Instance.Name, "CALL", new
        {
            instance = session.Instance.Name,
            data = calls
        });
    }

    // ── Persistence helpers ───────────────────────────────────────────────────

    private async Task PersistMessageAsync(string instanceId, JsonElement msgElement)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<MessageStoreService>();
            await store.UpsertMessageAsync(instanceId, msgElement, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not persist inbound message for instance {Id}", instanceId);
        }
    }

    private async Task PersistDeleteMessageAsync(string instanceId, string[] ids, string? jid, CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<MessageStoreService>();
            await store.DeleteMessagesAsync(instanceId, ids, jid, ct);
        }
        catch { }
    }

    private async Task PersistMarkReadAsync(string instanceId, string jid, CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<MessageStoreService>();
            await store.MarkChatReadAsync(instanceId, jid, ct);
        }
        catch { }
    }

    private async Task PersistProfileNameAsync(string instanceId, string name, CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Instances.Where(x => x.Id == instanceId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProfileName, name), ct);
        }
        catch { }
    }

    private async Task SaveConnectionStateAsync(string instanceId, string state, CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Instances.Where(x => x.Id == instanceId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConnectionStatus, state), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not persist connection state for instance {Id}", instanceId);
        }
    }

    private void QueueWebhook(string instanceName, string eventName, object data)
    {
        try
        {
            var json = JsonSerializer.SerializeToElement(data, JsonOpts);
            _ = webhooks.DispatchAsync(instanceName, eventName, json, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Webhook dispatch failed for {Event} on {Instance}", eventName, instanceName);
        }
    }

    private async Task<Instance> FindInstanceAsync(string instanceId, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Instances.AsNoTracking().SingleOrDefaultAsync(x => x.Id == instanceId, ct)
               ?? throw new KeyNotFoundException("Instance was not found.");
    }

    private WaSocket GetSocket(string instanceId)
    {
        if (!_sessions.TryGetValue(instanceId, out var session))
            throw new InvalidOperationException($"Instance '{instanceId}' is not running in this process.");
        if (session.State != "open")
            throw new InvalidOperationException($"Instance '{instanceId}' is not connected (state: {session.State}).");
        return new WaSocket(session.Client);
    }

    // ── Inner types ────────────────────────────────────────────────────────────

    private sealed class ClientSession(
        Instance instance,
        BaileysClient client,
        SqlAuthStateProvider authProvider,
        SqlSignalKeyStore signalKeys,
        BaileysEventEmitter ev)
    {
        public Instance Instance { get; } = instance;
        public BaileysClient Client { get; } = client;
        public SqlAuthStateProvider AuthProvider { get; } = authProvider;
        public SqlSignalKeyStore SignalKeys { get; } = signalKeys;
        public BaileysEventEmitter Ev { get; } = ev;
        public CancellationTokenSource Lifecycle { get; } = new();
        public TaskCompletionSource<bool> QrAvailable { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int ConnectionStarted;
        public string State = "close";
        public string? Qr;
        public string? QrBase64;
        public int QrCount;
    }

    private sealed class SqlAuthStateProvider(string instanceId, SessionKeyStore store) : IAuthStateProvider
    {
        private const string CredKey = "baileys-net.credentials";
        private static readonly JsonSerializerOptions Jo = new(JsonSerializerDefaults.Web);

        public async Task<AuthenticationCreds> LoadCredsAsync(CancellationToken ct)
        {
            var json = await store.ReadAsync(instanceId, CredKey, ct);
            return json is null ? AuthUtils.InitAuthCreds()
                : JsonSerializer.Deserialize<AuthenticationCreds>(json, Jo) ?? AuthUtils.InitAuthCreds();
        }

        public Task SaveCredsAsync(AuthenticationCreds creds, CancellationToken ct) =>
            store.SaveAsync(instanceId, CredKey, JsonSerializer.Serialize(creds, Jo), ct);

        public Task ClearAsync(CancellationToken ct) =>
            store.DeleteAllAsync(instanceId, ct);
    }
}
