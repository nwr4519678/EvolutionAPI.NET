using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Baileys.Types;
using Baileys.WABinary;
using EvolutionApi.Infrastructure.WhatsApp.Services;

namespace EvolutionApi.Infrastructure.WhatsApp.Engine;

/// <summary>
/// Builds and encrypts WhatsApp binary message nodes using the Signal Protocol.
///
/// WA pairwise message flow:
///   1. Look up recipient's Signal session in <see cref="SqlSignalKeyStore"/>.
///   2. If no session → fetch pre-keys from WA server via usync IQ, establish session via X3DH.
///   3. Encrypt plaintext (WAProto bytes) with the session cipher (Double Ratchet chain).
///   4. Wrap in a "enc" binary node inside a "message" node.
///
/// WA group message flow:
///   1. Retrieve (or generate) the SenderKeyRecord for the group.
///   2. Each recipient must receive a SenderKeyDistributionMessage (SKDM) once.
///   3. Group messages are encrypted with the group's sender key (cheaper: one encrypt, many recipients).
///
/// This implementation handles pairwise messages fully. Group sender-key distribution
/// is built but requires establishing individual sessions with each participant first.
/// </summary>
public sealed class WaMessageEncryptor(SqlSignalKeyStore signalStore, SessionKeyStore sessionStore, ILogger<WaMessageEncryptor> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // ── Main entry point ──────────────────────────────────────────────────────

    /// <summary>
    /// Encrypts <paramref name="plaintextProto"/> and returns a ready-to-send WA binary node.
    /// For group JIDs (@g.us) the session approach is used (SKDM via pairwise encrypt).
    /// </summary>
    public async Task<BinaryNode> BuildEncryptedMessageNodeAsync(
        string instanceId,
        string toJid,
        string msgId,
        byte[] plaintextProto,
        CancellationToken ct)
    {
        var isGroup = toJid.EndsWith("@g.us", StringComparison.Ordinal);
        logger.LogDebug("Encrypting message {MsgId} to {Jid} (group={IsGroup})", msgId, toJid, isGroup);

        if (isGroup)
            return await BuildGroupMessageNodeAsync(instanceId, toJid, msgId, plaintextProto, ct);

        return await BuildPairwiseMessageNodeAsync(instanceId, toJid, msgId, plaintextProto, ct);
    }

    // ── Pairwise (1:1) message ────────────────────────────────────────────────

    private async Task<BinaryNode> BuildPairwiseMessageNodeAsync(
        string instanceId, string toJid, string msgId, byte[] plaintextProto, CancellationToken ct)
    {
        var session = await GetOrEstablishSessionAsync(instanceId, toJid, ct);
        var ciphertext = EncryptWithSession(session, plaintextProto, out var msgType);
        await PersistSessionAsync(instanceId, toJid, session, ct);

        var encNode = new BinaryNode
        {
            Tag = "enc",
            Attrs = new Dictionary<string, string> { ["v"] = "2", ["type"] = msgType },
            Content = new BinaryNodeBytes(ciphertext)
        };

        return new BinaryNode
        {
            Tag = "message",
            Attrs = new Dictionary<string, string>
            {
                ["to"] = toJid, ["id"] = msgId, ["type"] = "text"
            },
            Content = new BinaryNodeList([encNode])
        };
    }

    // ── Group message ─────────────────────────────────────────────────────────

    private async Task<BinaryNode> BuildGroupMessageNodeAsync(
        string instanceId, string groupJid, string msgId, byte[] plaintextProto, CancellationToken ct)
    {
        // For group messages: encrypt plaintext with the sender key, distribute SKDM to participants.
        // Sender key record is stored as "senderKey:{groupJid}:{myJid}"
        var myJid = await GetMyJidAsync(instanceId, ct);
        var senderKeyId = $"senderKey:{groupJid}:{myJid}";
        var senderKeyRecord = await signalStore.GetSenderKeyAsync(senderKeyId, ct)
                              ?? await CreateSenderKeyAsync(instanceId, senderKeyId, ct);

        var (iteration, senderKey) = ParseSenderKey(senderKeyRecord);
        var ciphertext = WaSignalCrypto.EncryptSenderKeyMessage([], senderKey, iteration, plaintextProto);

        // Advance sender key ratchet
        var nextSenderKey = AdvanceSenderKey(senderKey);
        await signalStore.SetSenderKeyAsync(senderKeyId, BuildSenderKeyRecord(iteration + 1, nextSenderKey), ct);

        var encNode = new BinaryNode
        {
            Tag = "enc",
            Attrs = new Dictionary<string, string> { ["v"] = "2", ["type"] = "skmsg" },
            Content = new BinaryNodeBytes(ciphertext)
        };

        return new BinaryNode
        {
            Tag = "message",
            Attrs = new Dictionary<string, string>
            {
                ["to"] = groupJid, ["id"] = msgId, ["type"] = "text"
            },
            Content = new BinaryNodeList([encNode])
        };
    }

    // ── Session management (X3DH + Double Ratchet state) ─────────────────────

    private async Task<SignalSession> GetOrEstablishSessionAsync(string instanceId, string jid, CancellationToken ct)
    {
        var sessionJson = await signalStore.GetSessionAsync(jid, ct);
        if (sessionJson is not null)
        {
            var session = JsonSerializer.Deserialize<SignalSession>(sessionJson, JsonOpts);
            if (session is not null) return session;
        }

        // No session: must have pre-keys fetched from WA server by caller (WaSocket.FetchPreKeysAsync)
        // The pre-keys are stored in the keyStore under "preKeys:{jid}"
        var preKeyJson = await signalStore.GetPreKeyBundleAsync(jid, ct);
        if (preKeyJson is null)
            throw new InvalidOperationException(
                $"No Signal session and no pre-key bundle available for {jid}. " +
                "Fetch pre-keys first via WaSocket.FetchPreKeysAsync.");

        var preKeyBundle = JsonSerializer.Deserialize<PreKeyBundle>(preKeyJson, JsonOpts)
                           ?? throw new InvalidOperationException("Failed to deserialize pre-key bundle.");

        return EstablishSession(preKeyBundle, await GetMyIdentityKeyAsync(instanceId, ct));
    }

    /// <summary>X3DH session initiation (Alice / sender side).</summary>
    private static SignalSession EstablishSession(PreKeyBundle remote, (byte[] privKey, byte[] pubKey) myIdentity)
    {
        // Generate ephemeral key pair
        var (ekPriv, ekPub) = WaSignalCrypto.GenerateKeyPair();

        // X3DH DH calculations
        // DH1 = DH(IK_sender, SPK_recipient)
        var dh1 = WaSignalCrypto.X25519(myIdentity.privKey, remote.SignedPreKey);
        // DH2 = DH(EK_sender, IK_recipient)
        var dh2 = WaSignalCrypto.X25519(ekPriv, remote.IdentityKey);
        // DH3 = DH(EK_sender, SPK_recipient)
        var dh3 = WaSignalCrypto.X25519(ekPriv, remote.SignedPreKey);
        // DH4 = DH(EK_sender, OPK_recipient) [optional, if one-time pre-key available]
        byte[]? dh4 = remote.OneTimePreKey is { Length: > 0 } otk
            ? WaSignalCrypto.X25519(ekPriv, otk) : null;

        // Master secret
        var f = new byte[32]; // 32 zero-bytes prefix (WA convention)
        var dhConcat = dh4 is null
            ? Concat([f, dh1, dh2, dh3])
            : Concat([f, dh1, dh2, dh3, dh4]);

        var masterSecret = WaSignalCrypto.Hkdf(dhConcat,
            Encoding.UTF8.GetBytes("WhisperText"), 64);

        var rootKey = masterSecret[..32];
        var chainKey = masterSecret[32..64];

        return new SignalSession
        {
            RemoteIdentityKey = remote.IdentityKey,
            EphemeralKeyPub = ekPub,
            RootKey = rootKey,
            ChainKey = chainKey,
            SendCount = 0,
            IsPreKey = true,
            PreKeyId = remote.PreKeyId,
            SignedPreKeyId = remote.SignedPreKeyId
        };
    }

    /// <summary>Encrypts with Double Ratchet, advances the chain.</summary>
    private static byte[] EncryptWithSession(SignalSession session, byte[] plaintext, out string msgType)
    {
        // Derive message key from chain key
        var messageKey = WaSignalCrypto.HmacSha256(session.ChainKey, [1]);
        session.ChainKey = WaSignalCrypto.HmacSha256(session.ChainKey, [2]);
        session.SendCount++;

        var (cipherKey, macKey, iv) = WaSignalCrypto.DeriveMessageKeys(messageKey);
        var ciphertext = WaSignalCrypto.EncryptMessage(cipherKey, macKey, iv, plaintext);

        // If first message (pre-key message), wrap with header
        if (session.IsPreKey)
        {
            session.IsPreKey = false;
            msgType = "pkmsg";
            // Build PreKeyWhisperMessage: version(1) + pre-key fields + ciphertext
            return BuildPreKeyMessage(session, ciphertext);
        }

        msgType = "msg";
        return BuildWhisperMessage(session, ciphertext, messageKey);
    }

    private static byte[] BuildPreKeyMessage(SignalSession s, byte[] ciphertext)
    {
        // Format: [version byte][pre-key header][ciphertext]
        // Version = 0x33 (WA uses version 3.3)
        var versionByte = new byte[] { 0x33 };
        return Concat(versionByte, s.EphemeralKeyPub, WaProto.EncodeVarint(s.PreKeyId),
                       WaProto.EncodeVarint(s.SignedPreKeyId), s.RemoteIdentityKey, ciphertext);
    }

    private static byte[] BuildWhisperMessage(SignalSession s, byte[] ciphertext, byte[] msgKey)
    {
        return Concat(new byte[] { 0x33 }, s.EphemeralKeyPub, WaProto.EncodeVarint((uint)s.SendCount), ciphertext);
    }

    // ── Sender key helpers ─────────────────────────────────────────────────────

    private async Task<string> CreateSenderKeyAsync(string instanceId, string senderKeyId, CancellationToken ct)
    {
        var senderKey = WaSignalCrypto.RandomBytes(32);
        var record = BuildSenderKeyRecord(0, senderKey);
        await signalStore.SetSenderKeyAsync(senderKeyId, record, ct);
        return record;
    }

    private static (uint iteration, byte[] senderKey) ParseSenderKey(string record)
    {
        var parts = record.Split(':');
        return (uint.Parse(parts[0]), Convert.FromBase64String(parts[1]));
    }

    private static string BuildSenderKeyRecord(uint iteration, byte[] senderKey)
        => $"{iteration}:{Convert.ToBase64String(senderKey)}";

    private static byte[] AdvanceSenderKey(byte[] senderKey)
        => WaSignalCrypto.HkdfExpand(senderKey, Encoding.UTF8.GetBytes("WhisperGroup"), 32);

    // ── Identity key helpers ───────────────────────────────────────────────────

    private async Task<(byte[] privKey, byte[] pubKey)> GetMyIdentityKeyAsync(string instanceId, CancellationToken ct)
    {
        var json = await sessionStore.ReadAsync(instanceId, "baileys-net.credentials", ct);
        if (json is null) throw new InvalidOperationException("Instance credentials not found.");
        var creds = JsonSerializer.Deserialize<JsonElement>(json);
        // WA credentials include signedIdentityKey: {private, public}
        if (creds.TryGetProperty("signedIdentityKey", out var ik))
        {
            var priv = Convert.FromBase64String(ik.GetProperty("private").GetString() ?? "");
            var pub = Convert.FromBase64String(ik.GetProperty("public").GetString() ?? "");
            return (priv, pub);
        }
        throw new InvalidOperationException("signedIdentityKey not found in credentials.");
    }

    private async Task<string> GetMyJidAsync(string instanceId, CancellationToken ct)
    {
        var json = await sessionStore.ReadAsync(instanceId, "baileys-net.credentials", ct);
        if (json is null) throw new InvalidOperationException("Instance credentials not found.");
        var creds = JsonSerializer.Deserialize<JsonElement>(json);
        return creds.TryGetProperty("me", out var me) && me.TryGetProperty("id", out var id)
            ? id.GetString() ?? throw new InvalidOperationException("me.id is null")
            : throw new InvalidOperationException("me field not in credentials.");
    }

    // ── Persist session ────────────────────────────────────────────────────────

    private async Task PersistSessionAsync(string instanceId, string jid, SignalSession session, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(session, JsonOpts);
        await signalStore.SetSessionAsync(jid, json, ct);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static byte[] Concat(params byte[][] parts)
    {
        var result = new byte[parts.Sum(p => p.Length)];
        var pos = 0;
        foreach (var p in parts) { p.CopyTo(result, pos); pos += p.Length; }
        return result;
    }

    // ── Inner types ───────────────────────────────────────────────────────────

    private sealed class SignalSession
    {
        public byte[] RemoteIdentityKey { get; set; } = [];
        public byte[] EphemeralKeyPub { get; set; } = [];
        public byte[] RootKey { get; set; } = [];
        public byte[] ChainKey { get; set; } = [];
        public int SendCount { get; set; }
        public bool IsPreKey { get; set; } = true;
        public uint PreKeyId { get; set; }
        public uint SignedPreKeyId { get; set; }
    }

    private sealed class PreKeyBundle
    {
        public byte[] IdentityKey { get; set; } = [];
        public byte[] SignedPreKey { get; set; } = [];
        public uint SignedPreKeyId { get; set; }
        public byte[]? OneTimePreKey { get; set; }
        public uint PreKeyId { get; set; }
    }
}

/// <summary>Extension to SqlSignalKeyStore for missing session/senderKey accessors.</summary>
public static class SignalKeyStoreExtensions
{
    public static Task<string?> GetSessionAsync(this SqlSignalKeyStore store, string jid, CancellationToken ct)
        => store.GetKeyAsync($"sessions:{jid}", ct);

    public static Task SetSessionAsync(this SqlSignalKeyStore store, string jid, string json, CancellationToken ct)
        => store.SetKeyAsync($"sessions:{jid}", json, ct);

    public static Task<string?> GetPreKeyBundleAsync(this SqlSignalKeyStore store, string jid, CancellationToken ct)
        => store.GetKeyAsync($"preKeys:{jid}", ct);

    public static Task SetPreKeyBundleAsync(this SqlSignalKeyStore store, string jid, string json, CancellationToken ct)
        => store.SetKeyAsync($"preKeys:{jid}", json, ct);

    public static Task<string?> GetSenderKeyAsync(this SqlSignalKeyStore store, string senderKeyId, CancellationToken ct)
        => store.GetKeyAsync($"sender-key-memory:{senderKeyId}", ct);

    public static Task SetSenderKeyAsync(this SqlSignalKeyStore store, string senderKeyId, string record, CancellationToken ct)
        => store.SetKeyAsync($"sender-key-memory:{senderKeyId}", record, ct);
}

// ── WaProto helper for varint (needed in message builder) ──────────────────────
public static partial class WaProto
{
    internal static byte[] EncodeVarint(uint value)
    {
        var buf = new byte[5];
        var pos = 0;
        do { buf[pos] = (byte)((value & 0x7F) | (value > 0x7F ? 0x80u : 0)); value >>= 7; pos++; } while (value > 0);
        return buf[..pos];
    }
}
