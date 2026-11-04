using System.Security.Cryptography;
using System.Text.Json;
using Baileys.Types;
using Baileys.WABinary;
using EvolutionApi.Infrastructure.WhatsApp.Services;

namespace EvolutionApi.Infrastructure.WhatsApp.Engine;

/// <summary>
/// Handles WhatsApp media operations:
///  1. Fetching a media upload URL from the WA server (via binary node IQ).
///  2. Encrypting media using AES-256-CBC with a derived media key (Signal media key derivation).
///  3. Uploading the encrypted bytes to the WA CDN (media.cdn.whatsapp.net).
///  4. Fetching pre-key bundles for Signal session establishment.
///
/// Reference: Baileys source: src/Utils/messages-media.ts
/// </summary>
public sealed class WaMediaUploader(
    IHttpClientFactory httpFactory,
    ILogger<WaMediaUploader> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // ── WA media type helpers ──────────────────────────────────────────────────

    private static string MediaTypeString(string messageType) => messageType switch
    {
        "imageMessage" => "Image",
        "videoMessage" => "Video",
        "audioMessage" => "Audio",
        "documentMessage" => "Document",
        "stickerMessage" => "Image", // stickers use Image CDN
        _ => "Image"
    };

    // ── Upload flow ────────────────────────────────────────────────────────────

    /// <summary>
    /// Encrypts and uploads media bytes. Returns a populated <see cref="MediaPayload"/>
    /// ready to embed into a WA proto message.
    /// </summary>
    public async Task<MediaPayload> UploadMediaAsync(
        WaSocket socket,
        byte[] mediaBytes,
        string messageType,
        string mimeType,
        string? fileName,
        CancellationToken ct)
    {
        // 1. Encrypt
        var (encrypted, fileSha256, fileEncSha256, mediaKey) =
            WaSignalCrypto.EncryptMedia(mediaBytes, MediaTypeString(messageType));

        // 2. Fetch upload URL from WA
        var (url, directPath, mediaKeyTimestamp) =
            await FetchUploadUrlAsync(socket, messageType, fileEncSha256, encrypted.Length, ct);

        // 3. Upload to CDN
        await UploadToCdnAsync(url, encrypted, mimeType, ct);

        return new MediaPayload
        {
            Url = url,
            DirectPath = directPath,
            MimeType = mimeType,
            FileSha256 = fileSha256,
            FileEncSha256 = fileEncSha256,
            MediaKey = mediaKey,
            FileLength = mediaBytes.Length,
            MediaKeyTimestamp = mediaKeyTimestamp,
            FileName = fileName
        };
    }

    // ── Fetch upload URL via WA IQ ─────────────────────────────────────────────

    private async Task<(string url, string directPath, long mediaKeyTimestamp)> FetchUploadUrlAsync(
        WaSocket socket,
        string messageType,
        byte[] fileEncSha256,
        int fileSize,
        CancellationToken ct)
    {
        // Build the media upload IQ node (w:m xmlns)
        var mediaNode = new BinaryNode
        {
            Tag = "iq",
            Attrs = new Dictionary<string, string>
            {
                ["id"] = RandomHex(6), ["to"] = "s.whatsapp.net",
                ["xmlns"] = "w:m", ["type"] = "set"
            },
            Content = new BinaryNodeList([
                new BinaryNode
                {
                    Tag = "media",
                    Attrs = new Dictionary<string, string>
                    {
                        ["type"] = MediaTypeString(messageType).ToLowerInvariant(),
                        ["hash"] = Convert.ToBase64String(fileEncSha256),
                        ["size"] = fileSize.ToString()
                    },
                    Content = null
                }
            ])
        };

        try
        {
            var result = await socket.QueryAsync(mediaNode, ct);
            var media = BinaryNodeExtensions.GetChild(result, "media");
            if (media is null) throw new InvalidOperationException("No media node in upload URL response.");

            var url = media.Attrs.TryGetValue("url", out var u) ? u : throw new InvalidOperationException("No url in media upload response.");
            var directPath = media.Attrs.TryGetValue("direct_path", out var dp) ? dp : string.Empty;
            var ts = media.Attrs.TryGetValue("media_key_timestamp", out var t) && long.TryParse(t, out var l) ? l : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (url, directPath, ts);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch media upload URL from WA server");
            throw new InvalidOperationException($"Could not get media upload URL: {ex.Message}", ex);
        }
    }

    // ── Upload to WhatsApp CDN ─────────────────────────────────────────────────

    private async Task UploadToCdnAsync(string url, byte[] encrypted, string mimeType, CancellationToken ct)
    {
        using var client = httpFactory.CreateClient("media");
        using var content = new ByteArrayContent(encrypted);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        content.Headers.Add("Origin", "https://web.whatsapp.com");
        content.Headers.Add("Referer", "https://web.whatsapp.com/");
        var response = await client.PostAsync(url, content, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"WA CDN upload failed: {response.StatusCode}");
    }

    // ── Download + decrypt from WA CDN ────────────────────────────────────────

    /// <summary>
    /// Downloads and decrypts a media file from the WA CDN.
    /// </summary>
    public async Task<byte[]> DownloadMediaAsync(
        string url, byte[] mediaKey, string messageType, CancellationToken ct)
    {
        using var client = httpFactory.CreateClient("media");
        var encrypted = await client.GetByteArrayAsync(url, ct);
        return WaSignalCrypto.DecryptMedia(encrypted, mediaKey, MediaTypeString(messageType));
    }

    // ── Pre-key fetching for Signal session establishment ──────────────────────

    /// <summary>
    /// Fetches and stores pre-key bundles for one or more JIDs via the WA usync IQ.
    /// Must be called before sending the first message to a new contact.
    /// </summary>
    public async Task FetchAndStorePreKeysAsync(
        WaSocket socket,
        SqlSignalKeyStore signalStore,
        string[] jids,
        CancellationToken ct)
    {
        var contacts = jids.Select(j => new BinaryNode
        {
            Tag = "contact",
            Attrs = new Dictionary<string, string> { ["jid"] = j },
            Content = null
        }).ToArray();

        var usyncNode = new BinaryNode
        {
            Tag = "iq",
            Attrs = new Dictionary<string, string>
            {
                ["id"] = RandomHex(6), ["to"] = "s.whatsapp.net",
                ["xmlns"] = "usync", ["type"] = "get"
            },
            Content = new BinaryNodeList([
                new BinaryNode
                {
                    Tag = "usync",
                    Attrs = new Dictionary<string, string>
                    {
                        ["sid"] = RandomHex(6), ["mode"] = "query", ["last"] = "true",
                        ["index"] = "0", ["context"] = "message"
                    },
                    Content = new BinaryNodeList([
                        new BinaryNode
                        {
                            Tag = "query",
                            Attrs = [],
                            Content = new BinaryNodeList([
                                new BinaryNode { Tag = "devices", Attrs = new() { ["version"] = "2" }, Content = null }
                            ])
                        },
                        new BinaryNode { Tag = "list", Attrs = [], Content = new BinaryNodeList(contacts) }
                    ])
                }
            ])
        };

        BinaryNode result;
        try { result = await socket.QueryAsync(usyncNode, ct); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch pre-keys for {Count} JIDs", jids.Length);
            return;
        }

        var usyncResult = BinaryNodeExtensions.GetChild(result, "usync");
        var list = usyncResult is null ? null : BinaryNodeExtensions.GetChild(usyncResult, "list");
        if (list is null) return;

        foreach (var user in BinaryNodeExtensions.GetChildren(list))
        {
            if (!user.Attrs.TryGetValue("jid", out var jid)) continue;
            var devices = BinaryNodeExtensions.GetChild(user, "devices");
            if (devices is null) continue;

            var bundle = ParsePreKeyBundle(jid, devices);
            if (bundle is not null)
                await signalStore.SetPreKeyBundleAsync(jid, JsonSerializer.Serialize(bundle, JsonOpts), ct);
        }
    }

    // ── Pre-key bundle parser ─────────────────────────────────────────────────

    private static PreKeyBundleDto? ParsePreKeyBundle(string jid, BinaryNode devicesNode)
    {
        // The device list contains encrypted pre-key data from WA.
        // In the actual protocol these come as binary JIDPAIR encoded nodes.
        // We extract identity key, signed pre-key, and one-time pre-key.
        var deviceList = BinaryNodeExtensions.GetChild(devicesNode, "device-list");
        var firstDevice = deviceList is null ? null : BinaryNodeExtensions.GetChildren(deviceList).FirstOrDefault();
        if (firstDevice is null) return null;

        // The identity key comes from the keyBundle sub-node
        var keyBundle = BinaryNodeExtensions.GetChild(firstDevice, "key-bundle");
        if (keyBundle is null) return null;

        var identityKey = GetNodeBytes(keyBundle, "identity");
        var spKey = GetNodeBytes(keyBundle, "skey");
        var preKey = GetNodeBytes(keyBundle, "key");

        if (identityKey is null || spKey is null) return null;

        return new PreKeyBundleDto
        {
            Jid = jid,
            IdentityKey = identityKey,
            SignedPreKey = spKey[4..36], // skip 4-byte id, take 32-byte key
            SignedPreKeyId = spKey.Length >= 4 ? (uint)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(spKey[..4]) : 0,
            OneTimePreKey = preKey?.Length >= 36 ? preKey[4..36] : null,
            PreKeyId = preKey?.Length >= 4 ? (uint)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(preKey[..4]) : 0
        };
    }

    private static byte[]? GetNodeBytes(BinaryNode parent, string childTag)
    {
        var child = BinaryNodeExtensions.GetChild(parent, childTag);
        return child?.Content is BinaryNodeBytes bnb ? bnb.Data : null;
    }

    private static string RandomHex(int bytes)
    {
        Span<byte> buf = stackalloc byte[bytes];
        RandomNumberGenerator.Fill(buf);
        return Convert.ToHexString(buf).ToLowerInvariant();
    }

    private sealed class PreKeyBundleDto
    {
        public string Jid { get; set; } = null!;
        public byte[] IdentityKey { get; set; } = [];
        public byte[] SignedPreKey { get; set; } = [];
        public uint SignedPreKeyId { get; set; }
        public byte[]? OneTimePreKey { get; set; }
        public uint PreKeyId { get; set; }
    }
}
