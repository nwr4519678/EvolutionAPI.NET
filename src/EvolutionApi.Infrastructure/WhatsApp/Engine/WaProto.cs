using System.Text;

namespace EvolutionApi.Infrastructure.WhatsApp.Engine;

/// <summary>
/// Hand-coded minimal protobuf serializer covering the WhatsApp message proto fields
/// needed for sending. Uses known field numbers from the reverse-engineered WAProto schema.
/// Reference: https://github.com/WhiskeySockets/Baileys/blob/master/proto/def.proto
/// </summary>
public static partial class WaProto
{
    // ── Field numbers (WAProto.Message) ────────────────────────────────────────

    // Message.conversation            = 1   (string)
    // Message.senderKeyDistMsg        = 2   (bytes)
    // Message.imageMessage            = 3   (sub-message)
    // Message.contactMessage          = 4   (sub-message)
    // Message.locationMessage         = 5   (sub-message)
    // Message.extendedTextMessage     = 6   (sub-message)
    // Message.documentMessage         = 7   (sub-message)
    // Message.audioMessage            = 8   (sub-message)
    // Message.videoMessage            = 9   (sub-message)
    // Message.stickerMessage          = 20  (sub-message)
    // Message.reactionMessage         = 22  (sub-message)
    // Message.listMessage             = 28  (sub-message)
    // Message.pollCreationMessage     = 45  (sub-message)

    // ExtendedTextMessage.text        = 1   (string)
    // ExtendedTextMessage.description = 2   (string)
    // ExtendedTextMessage.contextInfo = 17  (sub-message)

    // ContextInfo.stanzaId            = 1   (string)
    // ContextInfo.participant         = 2   (string)
    // ContextInfo.quotedMessage       = 3   (sub-message)
    // ContextInfo.mentionedJid        = 15  (repeated string)

    // MediaMessage (common fields):
    // .url                            = 1   (string)
    // .mimetype                       = 2   (string)
    // .fileSha256                     = 3   (bytes)
    // .fileLength                     = 4   (uint64)
    // .mediaKey                       = 5   (bytes)
    // .fileEncSha256                  = 8   (bytes)
    // .directPath                     = 16  (string)
    // .mediaKeyTimestamp              = 17  (int64)
    // .caption                        = 6   (string) [image/video]

    // ReactionMessage.key             = 1   (sub-message)
    // ReactionMessage.text            = 2   (string)

    // MessageKey.remoteJid            = 1   (string)
    // MessageKey.fromMe               = 2   (bool)
    // MessageKey.id                   = 3   (string)
    // MessageKey.participant          = 4   (string)

    // ── Encode text message ────────────────────────────────────────────────────

    /// <summary>Encodes a conversation (plain text) message.</summary>
    public static byte[] EncodeText(string text, ContextInfo? ctx = null)
    {
        if (ctx is null)
            return WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(text));

        // Use extendedTextMessage (field 6) when there's a context
        var inner = new List<byte[]>();
        inner.Add(WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(text)));
        var ctxBytes = EncodeContextInfo(ctx);
        inner.Add(WriteField(17, WireType.LengthDelimited, ctxBytes));
        return WriteField(6, WireType.LengthDelimited, Concat(inner));
    }

    /// <summary>Encodes a reaction message.</summary>
    public static byte[] EncodeReaction(string remoteJid, bool fromMe, string msgId, string emoji)
    {
        var key = new List<byte[]>
        {
            WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(remoteJid)),
            WriteField(2, WireType.Varint, fromMe ? [1] : [0]),
            WriteField(3, WireType.LengthDelimited, Encoding.UTF8.GetBytes(msgId))
        };
        return WriteField(22, WireType.LengthDelimited,
            Concat([
                WriteField(1, WireType.LengthDelimited, Concat(key)),
                WriteField(2, WireType.LengthDelimited, Encoding.UTF8.GetBytes(emoji))
            ]));
    }

    /// <summary>Encodes a location message.</summary>
    public static byte[] EncodeLocation(double latitude, double longitude, string? name, string? address)
    {
        // locationMessage (field 5)
        // .degreesLatitude = 1 (double), .degreesLongitude = 2 (double)
        // .name = 3 (string), .address = 4 (string)
        var inner = new List<byte[]>
        {
            WriteField64(1, BitConverter.DoubleToInt64Bits(latitude)),
            WriteField64(2, BitConverter.DoubleToInt64Bits(longitude))
        };
        if (name is not null) inner.Add(WriteField(3, WireType.LengthDelimited, Encoding.UTF8.GetBytes(name)));
        if (address is not null) inner.Add(WriteField(4, WireType.LengthDelimited, Encoding.UTF8.GetBytes(address)));
        return WriteField(5, WireType.LengthDelimited, Concat(inner));
    }

    /// <summary>Encodes a contact vCard message.</summary>
    public static byte[] EncodeContact(string displayName, string vCard)
    {
        // contactMessage (field 4)
        // .displayName = 1 (string), .vcard = 16 (string)
        return WriteField(4, WireType.LengthDelimited, Concat([
            WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(displayName)),
            WriteField(16, WireType.LengthDelimited, Encoding.UTF8.GetBytes(vCard))
        ]));
    }

    /// <summary>
    /// Encodes a media message (image, video, document, audio, sticker).
    /// Field number depends on media type.
    /// </summary>
    public static byte[] EncodeMedia(int fieldNumber, MediaPayload media, string? caption = null)
    {
        var inner = new List<byte[]>
        {
            WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(media.Url ?? string.Empty)),
            WriteField(2, WireType.LengthDelimited, Encoding.UTF8.GetBytes(media.MimeType ?? "application/octet-stream")),
            WriteField(3, WireType.LengthDelimited, media.FileSha256),
            WriteField(4, WireType.Varint, EncodeVarint((ulong)media.FileLength)),
            WriteField(5, WireType.LengthDelimited, media.MediaKey),
            WriteField(8, WireType.LengthDelimited, media.FileEncSha256),
            WriteField(16, WireType.LengthDelimited, Encoding.UTF8.GetBytes(media.DirectPath ?? string.Empty)),
            WriteField64(17, media.MediaKeyTimestamp)
        };
        if (caption is not null)
            inner.Add(WriteField(6, WireType.LengthDelimited, Encoding.UTF8.GetBytes(caption)));
        if (media.FileName is not null)
            inner.Add(WriteField(30, WireType.LengthDelimited, Encoding.UTF8.GetBytes(media.FileName)));
        if (media.PageCount > 0)
            inner.Add(WriteField(10, WireType.Varint, EncodeVarint((ulong)media.PageCount)));
        return WriteField(fieldNumber, WireType.LengthDelimited, Concat(inner));
    }

    /// <summary>Encodes a poll creation message.</summary>
    public static byte[] EncodePoll(string name, string[] options, int selectableCount)
    {
        // pollCreationMessage (field 45)
        // .name = 1 (string), .options[].optionName = 1 (string) each, .selectableOptionsCount = 6 (uint32)
        var inner = new List<byte[]>
        {
            WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(name))
        };
        foreach (var opt in options)
            inner.Add(WriteField(2, WireType.LengthDelimited,
                WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(opt))));
        inner.Add(WriteField(6, WireType.Varint, EncodeVarint((ulong)selectableCount)));
        return WriteField(45, WireType.LengthDelimited, Concat(inner));
    }

    /// <summary>Encodes a list message (interactive buttons with list).</summary>
    public static byte[] EncodeList(string title, string description, string buttonText, ListSection[] sections, string? footer = null)
    {
        // listMessage (field 28)
        // .title=1, .description=2, .buttonText=3, .listType=4(varint:1=SINGLE_SELECT), .sections(5) -> rows(1)->{title,rowId,description}
        var inner = new List<byte[]>
        {
            WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(title)),
            WriteField(2, WireType.LengthDelimited, Encoding.UTF8.GetBytes(description)),
            WriteField(3, WireType.LengthDelimited, Encoding.UTF8.GetBytes(buttonText)),
            WriteField(4, WireType.Varint, EncodeVarint(1)) // SINGLE_SELECT
        };
        if (footer is not null)
            inner.Add(WriteField(6, WireType.LengthDelimited, Encoding.UTF8.GetBytes(footer)));
        foreach (var sec in sections)
        {
            var secBytes = new List<byte[]>();
            if (sec.Title is not null)
                secBytes.Add(WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(sec.Title)));
            foreach (var row in sec.Rows)
            {
                var rowBytes = new List<byte[]>
                {
                    WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(row.Title ?? string.Empty)),
                    WriteField(2, WireType.LengthDelimited, Encoding.UTF8.GetBytes(row.RowId ?? Guid.NewGuid().ToString("N")))
                };
                if (row.Description is not null)
                    rowBytes.Add(WriteField(3, WireType.LengthDelimited, Encoding.UTF8.GetBytes(row.Description)));
                secBytes.Add(WriteField(2, WireType.LengthDelimited, Concat(rowBytes)));
            }
            inner.Add(WriteField(5, WireType.LengthDelimited, Concat(secBytes)));
        }
        return WriteField(28, WireType.LengthDelimited, Concat(inner));
    }

    // ── Low-level protobuf helpers ─────────────────────────────────────────────

    private enum WireType { Varint = 0, Fixed64 = 1, LengthDelimited = 2, Fixed32 = 5 }

    private static byte[] WriteField(int fieldNumber, WireType wireType, byte[] data)
    {
        var tag = (uint)((fieldNumber << 3) | (int)wireType);
        var tagBytes = EncodeVarint(tag);
        var result = new byte[tagBytes.Length + data.Length];
        tagBytes.CopyTo(result, 0);
        data.CopyTo(result, tagBytes.Length);
        return result;
    }

    private static byte[] WriteField(int fieldNumber, WireType wireType, byte[] payloadBytes, bool withLength = true)
    {
        if (wireType != WireType.LengthDelimited) return WriteFieldRaw(fieldNumber, wireType, payloadBytes);
        var tag = (uint)((fieldNumber << 3) | (int)wireType);
        var tagBytes = EncodeVarint(tag);
        var lenBytes = EncodeVarint((ulong)payloadBytes.Length);
        var result = new byte[tagBytes.Length + lenBytes.Length + payloadBytes.Length];
        tagBytes.CopyTo(result, 0);
        lenBytes.CopyTo(result, tagBytes.Length);
        payloadBytes.CopyTo(result, tagBytes.Length + lenBytes.Length);
        return result;
    }

    private static byte[] WriteFieldRaw(int fieldNumber, WireType wireType, byte[] rawBytes)
    {
        var tag = EncodeVarint((uint)((fieldNumber << 3) | (int)wireType));
        return Concat([tag, rawBytes]);
    }

    private static byte[] WriteField64(int fieldNumber, long value)
    {
        var tag = EncodeVarint((uint)((fieldNumber << 3) | (int)WireType.Fixed64));
        var val = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian) Array.Reverse(val);
        return Concat([tag, val]);
    }

    private static byte[] EncodeVarint(ulong value)
    {
        var buf = new byte[10];
        var pos = 0;
        do { buf[pos] = (byte)((value & 0x7F) | (value > 0x7FUL ? 0x80UL : 0UL)); value >>= 7; pos++; } while (value > 0);
        return buf[..pos];
    }

    private static byte[] EncodeContextInfo(ContextInfo ctx)
    {
        var inner = new List<byte[]>();
        if (ctx.StanzaId is not null)
            inner.Add(WriteField(1, WireType.LengthDelimited, Encoding.UTF8.GetBytes(ctx.StanzaId)));
        if (ctx.Participant is not null)
            inner.Add(WriteField(2, WireType.LengthDelimited, Encoding.UTF8.GetBytes(ctx.Participant)));
        foreach (var jid in ctx.MentionedJids ?? [])
            inner.Add(WriteField(15, WireType.LengthDelimited, Encoding.UTF8.GetBytes(jid)));
        return Concat(inner);
    }

    private static byte[] Concat(IEnumerable<byte[]> parts)
    {
        var all = parts.ToArray();
        var result = new byte[all.Sum(a => a.Length)];
        var pos = 0;
        foreach (var a in all) { a.CopyTo(result, pos); pos += a.Length; }
        return result;
    }

    // ── MediaFieldNumber lookup ────────────────────────────────────────────────

    public static int MediaFieldNumber(string mediaType) => mediaType switch
    {
        "imageMessage" => 3,
        "documentMessage" => 7,
        "audioMessage" => 8,
        "videoMessage" => 9,
        "stickerMessage" => 20,
        _ => 3
    };
}

// ── Data transfer objects ──────────────────────────────────────────────────────

public sealed record ContextInfo(string? StanzaId = null, string? Participant = null, string[]? MentionedJids = null);

public sealed class MediaPayload
{
    public string? Url { get; set; }
    public string? DirectPath { get; set; }
    public string MimeType { get; set; } = "application/octet-stream";
    public byte[] FileSha256 { get; set; } = [];
    public byte[] FileEncSha256 { get; set; } = [];
    public byte[] MediaKey { get; set; } = [];
    public long FileLength { get; set; }
    public long MediaKeyTimestamp { get; set; }
    public string? FileName { get; set; }
    public int PageCount { get; set; }
}

public sealed class ListSection(string? title, ListRow[] rows)
{
    public string? Title { get; } = title;
    public ListRow[] Rows { get; } = rows;
}

public sealed class ListRow(string? title, string? rowId, string? description = null)
{
    public string? Title { get; } = title;
    public string? RowId { get; } = rowId;
    public string? Description { get; } = description;
}
