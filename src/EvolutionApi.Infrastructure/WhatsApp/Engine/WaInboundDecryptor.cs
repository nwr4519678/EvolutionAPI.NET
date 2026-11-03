using System.Text.Json;
using EvolutionApi.Infrastructure.WhatsApp.Services;

namespace EvolutionApi.Infrastructure.WhatsApp.Engine;

/// <summary>
/// Decrypts inbound WhatsApp messages received from the server.
///
/// WhatsApp delivers messages as encrypted binary nodes containing:
///   - type="pkmsg" → PreKeyWhisperMessage (first message from a sender)
///   - type="msg"   → WhisperMessage (ongoing ratcheted messages)
///   - type="skmsg" → SenderKeyMessage (group messages)
///
/// This class is called by BaileysNetWhatsAppEngine.OnMessagesUpsert() when
/// the raw encrypted bytes are available. Currently Baileys.NET 1.9.1 exposes
/// only MinimalMessage (key + timestamp) — this decryptor is ready to process
/// the ciphertext bytes once the library's message.upsert event exposes them,
/// or when hooked into the raw frame handler.
///
/// Signal protocol references:
///   https://signal.org/docs/specifications/doubleratchet/
///   https://signal.org/docs/specifications/x3dh/
/// </summary>
public sealed class WaInboundDecryptor(
    SqlSignalKeyStore keyStore,
    SessionKeyStore sessionStore)
{
    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Decrypts a single inbound message ciphertext and returns the protobuf-encoded
    /// plaintext <c>WebMessageInfo</c> bytes, ready for further deserialisation.
    /// </summary>
    /// <param name="senderJid">Full JID of the sender (e.g. "447700900000@s.whatsapp.net").</param>
    /// <param name="type">"pkmsg", "msg", or "skmsg".</param>
    /// <param name="ciphertext">Raw encrypted bytes from the &lt;enc&gt; binary node.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Decrypted plaintext protobuf bytes.</returns>
    public async Task<byte[]> DecryptAsync(string senderJid, string type, byte[] ciphertext, CancellationToken ct)
    {
        return type switch
        {
            "pkmsg" => await DecryptPreKeyMessageAsync(senderJid, ciphertext, ct),
            "msg"   => await DecryptWhisperMessageAsync(senderJid, ciphertext, ct),
            "skmsg" => await DecryptSenderKeyMessageAsync(senderJid, ciphertext, ct),
            _       => throw new NotSupportedException($"Unknown message enc type: {type}")
        };
    }

    /// <summary>
    /// Decrypts a notification-encrypted message (used for retries / receipts).
    /// Uses the media/app key derivation rather than Signal session keys.
    /// </summary>
    public static byte[] DecryptNotification(byte[] ciphertext, byte[] key)
        => WaSignalCrypto.DecryptMedia(ciphertext, key, "History");

    // ── PreKeyWhisperMessage (first inbound message) ──────────────────────────

    private async Task<byte[]> DecryptPreKeyMessageAsync(string senderJid, byte[] data, CancellationToken ct)
    {
        // Binary layout (WA 3.3 protocol):
        //  [0]       version byte (0x33)
        //  [1..n]    pre-key header (varint preKeyId, varint signedPreKeyId)
        //  [n..n+32] sender's ephemeral public key (EC)
        //  [n+32..]  sender's identity key (32 bytes) + ciphertext
        if (data.Length < 65) throw new ArgumentException("PreKeyWhisperMessage too short", nameof(data));

        var version = data[0];  // should be 0x33
        var pos = 1;

        var preKeyId = ReadVarint(data, ref pos);
        var signedPreKeyId = ReadVarint(data, ref pos);

        var senderEphemeral = data[pos..(pos + 32)]; pos += 32;
        var senderIdentity  = data[pos..(pos + 32)]; pos += 32;
        var ciphertext      = data[pos..];

        // Load our signed pre-key private key
        var spkJson = await keyStore.GetKeyAsync("signedPreKey", (int)signedPreKeyId, ct)
            ?? throw new InvalidOperationException($"SignedPreKey {signedPreKeyId} not found");
        var spkPair = JsonSerializer.Deserialize<KeyPairRecord>(spkJson)!;
        var spkPriv = Convert.FromBase64String(spkPair.Private);

        // Load our one-time pre-key if referenced
        byte[]? opkPriv = null;
        if (preKeyId > 0)
        {
            var opkJson = await keyStore.GetKeyAsync("preKey", (int)preKeyId, ct);
            if (opkJson is not null)
            {
                var opkPair = JsonSerializer.Deserialize<KeyPairRecord>(opkJson)!;
                opkPriv = Convert.FromBase64String(opkPair.Private);
            }
        }

        // Load our identity key pair
        var idJson = await sessionStore.ReadAsync("_identity", "identityKeyPair", ct)
            ?? throw new InvalidOperationException("Identity key pair not found in session store");
        var idPair = JsonSerializer.Deserialize<KeyPairRecord>(idJson)!;
        var idPriv  = Convert.FromBase64String(idPair.Private);
        var idPub   = Convert.FromBase64String(idPair.Public);

        // X3DH Agreement (receiving side):
        //   DH1 = DH(IK_recv, EK_send)
        //   DH2 = DH(SPK_recv, IK_send)
        //   DH3 = DH(SPK_recv, EK_send)
        //   DH4 = DH(OPK_recv, EK_send)  [if one-time pre-key was used]
        var dh1 = WaSignalCrypto.DhAgreement(idPriv, senderEphemeral);
        var dh2 = WaSignalCrypto.DhAgreement(spkPriv, senderIdentity);
        var dh3 = WaSignalCrypto.DhAgreement(spkPriv, senderEphemeral);

        var dhInput = opkPriv is not null
            ? Concat(dh1, dh2, dh3, WaSignalCrypto.DhAgreement(opkPriv, senderEphemeral))
            : Concat(dh1, dh2, dh3);

        // Derive root key via HKDF (F = 0xFF * 32 as salt per X3DH spec)
        var hkdfSalt = new byte[32];
        Array.Fill(hkdfSalt, (byte)0xFF);
        var rootKey = WaSignalCrypto.Hkdf(dhInput,
            System.Text.Encoding.UTF8.GetBytes("WhisperText"), 32, hkdfSalt);

        // Derive message key: advance chain key, get msg key
        var chainKey = WaSignalCrypto.Hkdf(rootKey,
            System.Text.Encoding.UTF8.GetBytes("WhisperRatchet"), 32);
        var msgKeyRaw = WaSignalCrypto.HmacSha256(chainKey, [0x01]);
        var msgKey = WaSignalCrypto.Hkdf(msgKeyRaw,
            System.Text.Encoding.UTF8.GetBytes("WhisperMessageKeys"), 48);

        var plaintext = WaSignalCrypto.AesCbcDecrypt(
            msgKey[..32],    // key
            msgKey[32..48],  // IV
            ciphertext);

        // Consume the one-time pre-key (prevent replay)
        if (preKeyId > 0)
            await keyStore.DeleteKeyAsync("preKey", (int)preKeyId, ct);

        var recvChainKey = WaSignalCrypto.Hkdf(rootKey,
            System.Text.Encoding.UTF8.GetBytes("WhisperRatchet"), 32);
        await SaveSessionAsync(senderJid, new SignalInboundSession
        {
            RemoteIdentityKey = senderIdentity,
            RemoteEphemeralKey = senderEphemeral,
            RootKey = rootKey,
            RecvChainKey = recvChainKey,
            RecvCount = 1
        }, ct);

        return plaintext;
    }

    // ── WhisperMessage (ongoing ratchet) ─────────────────────────────────────

    private async Task<byte[]> DecryptWhisperMessageAsync(string senderJid, byte[] data, CancellationToken ct)
    {
        if (data.Length < 33) throw new ArgumentException("WhisperMessage too short", nameof(data));

        var pos = 1; // skip version byte
        var senderEphemeral = data[pos..(pos + 32)]; pos += 32;
        var msgCount = (uint)ReadVarint(data, ref pos);
        var ciphertext = data[pos..];

        var sessionJson = await keyStore.GetKeyAsync("session", senderJid, ct)
            ?? throw new InvalidOperationException($"No Signal session found for {senderJid}. Expecting pkmsg first.");
        var session = JsonSerializer.Deserialize<SignalInboundSession>(sessionJson)!;

        // Advance chain key to the message index
        var chainKey = session.RecvChainKey;
        for (var i = session.RecvCount; i < msgCount; i++)
            chainKey = WaSignalCrypto.AdvanceChainKey(chainKey);

        var msgKeyRaw = WaSignalCrypto.HmacSha256(chainKey, [0x01]);
        var msgKey = WaSignalCrypto.Hkdf(msgKeyRaw,
            System.Text.Encoding.UTF8.GetBytes("WhisperMessageKeys"), 48);
        var plaintext = WaSignalCrypto.AesCbcDecrypt(msgKey[..32], msgKey[32..48], ciphertext);

        // Update session state
        session.RecvChainKey = WaSignalCrypto.AdvanceChainKey(chainKey);
        session.RecvCount = (int)msgCount + 1;
        await SaveSessionAsync(senderJid, session, ct);

        return plaintext;
    }

    // ── SenderKeyMessage (group messages) ────────────────────────────────────

    private async Task<byte[]> DecryptSenderKeyMessageAsync(string senderJid, byte[] data, CancellationToken ct)
    {
        if (data.Length < 33) throw new ArgumentException("SenderKeyMessage too short", nameof(data));

        var pos = 1; // skip version
        var iteration = (uint)ReadVarint(data, ref pos);
        var ciphertext = data[pos..];

        // Load sender key for this JID (set when we received their SKDM)
        var skJson = await keyStore.GetKeyAsync("senderKey", senderJid, ct)
            ?? throw new InvalidOperationException($"No sender key for group member {senderJid}. Sender key distribution message may have been missed.");
        var sk = JsonSerializer.Deserialize<SenderKeyRecord>(skJson)!;

        // Advance ratchet to the correct iteration
        var chainKey = Convert.FromBase64String(sk.ChainKey);
        for (var i = sk.Iteration; i < iteration; i++)
            chainKey = WaSignalCrypto.AdvanceSenderChainKey(chainKey);

        var msgKey = WaSignalCrypto.DeriveSenderMessageKey(chainKey);
        var plaintext = WaSignalCrypto.AesCbcDecrypt(msgKey[..32], msgKey[32..48], ciphertext);

        // Update sender key state
        sk.ChainKey = Convert.ToBase64String(WaSignalCrypto.AdvanceSenderChainKey(chainKey));
        sk.Iteration = (int)iteration + 1;
        await keyStore.SetKeyAsync("senderKey", senderJid, JsonSerializer.Serialize(sk), ct);

        return plaintext;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SaveSessionAsync(string jid, SignalInboundSession session, CancellationToken ct)
        => await keyStore.SetKeyAsync("session", jid, JsonSerializer.Serialize(session), ct);

    private static byte[] Concat(params byte[][] parts)
    {
        var result = new byte[parts.Sum(p => p.Length)];
        var offset = 0;
        foreach (var p in parts) { Buffer.BlockCopy(p, 0, result, offset, p.Length); offset += p.Length; }
        return result;
    }

    private static ulong ReadVarint(byte[] data, ref int pos)
    {
        ulong value = 0;
        var shift = 0;
        while (pos < data.Length)
        {
            var b = data[pos++];
            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }
        return value;
    }

    // ── Session state models ──────────────────────────────────────────────────

    private sealed class SignalInboundSession
    {
        public byte[] RemoteIdentityKey { get; set; } = [];
        public byte[] RemoteEphemeralKey { get; set; } = [];
        public byte[] RootKey { get; set; } = [];
        public byte[] RecvChainKey { get; set; } = [];
        public int RecvCount { get; set; }
    }

    private sealed class SenderKeyRecord
    {
        public string ChainKey { get; set; } = "";
        public int Iteration { get; set; }
    }

    private sealed class KeyPairRecord
    {
        public string Public { get; set; } = "";
        public string Private { get; set; } = "";
    }
}
