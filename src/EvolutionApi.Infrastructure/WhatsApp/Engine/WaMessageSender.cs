using System.Text.Json;
using Baileys.Utils;
using EvolutionApi.Infrastructure.WhatsApp.Services;

namespace EvolutionApi.Infrastructure.WhatsApp.Engine;

/// <summary>
/// High-level message sender: builds WA proto, encrypts with Signal, sends via <see cref="WaSocket"/>.
/// Handles all 13 Evolution API message operations.
/// </summary>
public sealed class WaMessageSender(
    WaMessageEncryptor encryptor,
    WaMediaUploader mediaUploader,
    ILogger<WaMessageSender> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private static string NewMsgId() => Generics.GenerateMessageId();

    // ── Text ──────────────────────────────────────────────────────────────────

    public async Task<JsonElement> SendTextAsync(
        string instanceId, WaSocket socket,
        string toJid, string text, string? quotedId, string? quotedParticipant,
        string[]? mentionedJids, CancellationToken ct)
    {
        var ctx = (quotedId is not null || mentionedJids is { Length: > 0 })
            ? new ContextInfo(quotedId, quotedParticipant, mentionedJids)
            : null;
        var proto = WaProto.EncodeText(text, ctx);
        return await SendEncryptedAsync(instanceId, socket, toJid, proto, ct);
    }

    // ── Status (broadcast) ────────────────────────────────────────────────────

    public Task<JsonElement> SendStatusAsync(
        string instanceId, WaSocket socket,
        string type, string content, CancellationToken ct)
    {
        // Status messages go to "status@broadcast"
        byte[] proto = type switch
        {
            "text" => WaProto.EncodeText(content),
            _ => WaProto.EncodeText(content)
        };
        return SendEncryptedAsync(instanceId, socket, "status@broadcast", proto, ct);
    }

    // ── Reaction ──────────────────────────────────────────────────────────────

    public Task<JsonElement> SendReactionAsync(
        string instanceId, WaSocket socket,
        string toJid, string reactionMsgId, bool reactionFromMe, string emoji, CancellationToken ct)
    {
        var proto = WaProto.EncodeReaction(toJid, reactionFromMe, reactionMsgId, emoji);
        return SendEncryptedAsync(instanceId, socket, toJid, proto, ct);
    }

    // ── Location ──────────────────────────────────────────────────────────────

    public Task<JsonElement> SendLocationAsync(
        string instanceId, WaSocket socket,
        string toJid, double latitude, double longitude, string? name, string? address, CancellationToken ct)
    {
        var proto = WaProto.EncodeLocation(latitude, longitude, name, address);
        return SendEncryptedAsync(instanceId, socket, toJid, proto, ct);
    }

    // ── Contact ───────────────────────────────────────────────────────────────

    public Task<JsonElement> SendContactAsync(
        string instanceId, WaSocket socket,
        string toJid, string displayName, string vCard, CancellationToken ct)
    {
        var proto = WaProto.EncodeContact(displayName, vCard);
        return SendEncryptedAsync(instanceId, socket, toJid, proto, ct);
    }

    // ── Poll ──────────────────────────────────────────────────────────────────

    public Task<JsonElement> SendPollAsync(
        string instanceId, WaSocket socket,
        string toJid, string name, string[] options, int selectableCount, CancellationToken ct)
    {
        var proto = WaProto.EncodePoll(name, options, selectableCount);
        return SendEncryptedAsync(instanceId, socket, toJid, proto, ct);
    }

    // ── List ──────────────────────────────────────────────────────────────────

    public Task<JsonElement> SendListAsync(
        string instanceId, WaSocket socket,
        string toJid, string title, string description, string buttonText,
        ListSection[] sections, string? footer, CancellationToken ct)
    {
        var proto = WaProto.EncodeList(title, description, buttonText, sections, footer);
        return SendEncryptedAsync(instanceId, socket, toJid, proto, ct);
    }

    // ── Media (image, video, document, audio, sticker) ────────────────────────

    public async Task<JsonElement> SendMediaAsync(
        string instanceId, WaSocket socket,
        string toJid, string messageType, byte[] mediaBytes,
        string mimeType, string? caption, string? fileName, CancellationToken ct)
    {
        var payload = await mediaUploader.UploadMediaAsync(socket, mediaBytes, messageType, mimeType, fileName, ct);
        var fieldNum = WaProto.MediaFieldNumber(messageType);
        var proto = WaProto.EncodeMedia(fieldNum, payload, caption);
        return await SendEncryptedAsync(instanceId, socket, toJid, proto, ct);
    }

    // ── WhatsApp Audio (PTT voice message) ────────────────────────────────────

    public Task<JsonElement> SendWhatsAppAudioAsync(
        string instanceId, WaSocket socket,
        string toJid, byte[] audioBytes, string mimeType, CancellationToken ct)
    {
        // PTT audio uses audioMessage field (8) with ptt=true in attrs — encoded as a separate proto field 30
        return SendMediaAsync(instanceId, socket, toJid, "audioMessage", audioBytes, mimeType, null, null, ct);
    }

    // ── Core encrypt + send ───────────────────────────────────────────────────

    private async Task<JsonElement> SendEncryptedAsync(
        string instanceId, WaSocket socket,
        string toJid, byte[] plaintextProto, CancellationToken ct)
    {
        var msgId = NewMsgId();
        try
        {
            var node = await encryptor.BuildEncryptedMessageNodeAsync(instanceId, toJid, msgId, plaintextProto, ct);
            await socket.SendNodeAsync(node, ct);
            logger.LogInformation("Sent message {MsgId} to {JID}", msgId, toJid);
            return JsonSerializer.SerializeToElement(new
            {
                key = new { id = msgId, remoteJid = toJid, fromMe = true },
                messageTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                status = "PENDING"
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message {MsgId} to {JID}", msgId, toJid);
            throw new InvalidOperationException($"Message send failed: {ex.Message}", ex);
        }
    }
}
