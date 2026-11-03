using Baileys.Utils;
using System.Security.Cryptography;
using System.Text;

namespace EvolutionApi.Infrastructure.WhatsApp.Engine;

/// <summary>
/// WhatsApp Signal Protocol crypto helpers.
///
/// All primitive operations now delegate to Baileys.Utils.Crypto and AuthUtils from Baileys.NET,
/// and .NET's built-in HKDF — eliminating the BouncyCastle dependency for these operations.
///
/// Higher-level Signal Protocol compositions (DeriveMessageKeys, EncryptMessage, EncryptMedia, etc.)
/// remain here because Baileys.NET does not expose them as public API.
///
/// References:
///   https://signal.org/docs/specifications/x3dh/
///   https://signal.org/docs/specifications/doubleratchet/
///   https://github.com/WhiskeySockets/Baileys (make-noise.ts, signal.ts)
/// </summary>
public static class WaSignalCrypto
{
    // ── HKDF-SHA256 (.NET built-in HKDF class) ────────────────────────────────

    /// <summary>Performs HKDF-Expand using .NET built-in HKDF (no BouncyCastle needed).</summary>
    public static byte[] HkdfExpand(byte[] prk, byte[] info, int length)
        => HKDF.Expand(HashAlgorithmName.SHA256, prk, length, info);

    public static byte[] Hkdf(byte[] ikm, byte[] info, int length, byte[]? salt = null)
        => HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, length, salt ?? [], info);

    // ── AES-256-CBC (via Baileys.Utils.Crypto) ────────────────────────────────

    public static byte[] AesCbcEncrypt(byte[] key, byte[] iv, byte[] plaintext)
        => Crypto.AesEncryptWithIv(key, iv, plaintext);

    public static byte[] AesCbcDecrypt(byte[] key, byte[] iv, byte[] ciphertext)
        => Crypto.AesDecryptWithIv(key, iv, ciphertext);

    // ── HMAC-SHA256 (via Baileys.Utils.Crypto) ────────────────────────────────

    public static byte[] HmacSha256(byte[] key, byte[] data)
        => Crypto.HmacSha256(key, data);

    // ── Curve25519 / X25519 (via Baileys.Utils.AuthUtils) ────────────────────

    /// <summary>Generates a new X25519 key pair via Baileys.NET AuthUtils.</summary>
    public static (byte[] privateKey, byte[] publicKey) GenerateKeyPair()
    {
        var kp = AuthUtils.GenerateKeyPair();
        return (kp.Private, kp.Public);
    }

    /// <summary>X25519 Diffie-Hellman agreement via Baileys.NET AuthUtils.</summary>
    public static byte[] X25519(byte[] privateKey, byte[] publicKey)
        => AuthUtils.DiffieHellman(privateKey, publicKey);

    // ── Signal message encryption ──────────────────────────────────────────────
    // These are WhatsApp-specific compositions — no direct Baileys.NET public equivalent.

    /// <summary>
    /// Derives the 3-part message key (AES, MAC, IV) from a chain key step.
    /// Returns (cipherKey[32], macKey[32], iv[16]).
    /// </summary>
    public static (byte[] cipherKey, byte[] macKey, byte[] iv) DeriveMessageKeys(byte[] messageKey)
    {
        var expanded = Hkdf(messageKey, "WhisperMessageKeys"u8.ToArray(), 80);
        return (expanded[..32], expanded[32..64], expanded[64..80]);
    }

    /// <summary>Encrypt-then-MAC: AES-CBC encrypt, then HMAC-SHA256 over (iv || ciphertext). Returns ciphertext || mac[8].</summary>
    public static byte[] EncryptMessage(byte[] cipherKey, byte[] macKey, byte[] iv, byte[] plaintext)
    {
        var ct  = AesCbcEncrypt(cipherKey, iv, plaintext);
        var mac = HmacSha256(macKey, Concat(iv, ct))[..8];
        return Concat(ct, mac);
    }

    /// <summary>Verifies MAC then decrypts.</summary>
    public static byte[] DecryptMessage(byte[] cipherKey, byte[] macKey, byte[] iv, byte[] ciphertextWithMac)
    {
        var ct       = ciphertextWithMac[..^8];
        var mac      = ciphertextWithMac[^8..];
        var expected = HmacSha256(macKey, Concat(iv, ct))[..8];
        if (!CryptographicOperations.FixedTimeEquals(mac, expected))
            throw new CryptographicException("WhatsApp message MAC verification failed.");
        return AesCbcDecrypt(cipherKey, iv, ct);
    }

    // ── Media key derivation (for CDN upload/download) ─────────────────────────

    /// <summary>Derives media encryption keys from a 32-byte media key. Returns (iv[16], cipherKey[32], macKey[32]).</summary>
    public static (byte[] iv, byte[] cipherKey, byte[] macKey) DeriveMediaKeys(byte[] mediaKey, string mediaType)
    {
        var info    = Encoding.UTF8.GetBytes($"WhatsApp {mediaType} Keys");
        var derived = Hkdf(mediaKey, info, 112);
        return (derived[..16], derived[16..48], derived[48..80]);
    }

    /// <summary>Encrypts media bytes for WhatsApp CDN upload. Returns (encryptedBytes, fileSha256, fileEncSha256, mediaKey).</summary>
    public static (byte[] encrypted, byte[] fileSha256, byte[] fileEncSha256, byte[] mediaKey) EncryptMedia(
        byte[] plainBytes, string mediaType)
    {
        var mediaKey = RandomBytes(32);
        var (iv, cipherKey, macKey) = DeriveMediaKeys(mediaKey, mediaType);

        var fileSha256    = SHA256.HashData(plainBytes);
        var ciphertext    = AesCbcEncrypt(cipherKey, iv, plainBytes);
        var mac           = HmacSha256(macKey, Concat(iv, ciphertext))[..10];
        var encrypted     = Concat(ciphertext, mac);
        var fileEncSha256 = SHA256.HashData(encrypted);
        return (encrypted, fileSha256, fileEncSha256, mediaKey);
    }

    /// <summary>Decrypts media bytes received from WhatsApp CDN.</summary>
    public static byte[] DecryptMedia(byte[] encryptedBytes, byte[] mediaKey, string mediaType)
    {
        var (iv, cipherKey, macKey) = DeriveMediaKeys(mediaKey, mediaType);
        var ciphertext = encryptedBytes[..^10];
        var mac        = encryptedBytes[^10..];
        var expected   = HmacSha256(macKey, Concat(iv, ciphertext))[..10];
        if (!CryptographicOperations.FixedTimeEquals(mac, expected))
            throw new CryptographicException("WhatsApp media MAC verification failed.");
        return AesCbcDecrypt(cipherKey, iv, ciphertext);
    }

    // ── Sender key (group message encryption) ─────────────────────────────────

    public static byte[] EncryptSenderKeyMessage(byte[] signingKey, byte[] senderKey, uint iteration, byte[] plaintext)
    {
        var messageKey          = Hkdf(senderKey, "WhisperGroup"u8.ToArray(), 48);
        var (cipherKey, macKey, iv) = (messageKey[..32], messageKey[32..48], RandomBytes(16));
        return EncryptMessage(cipherKey, macKey, iv, plaintext);
    }

    // ── Padding (PKCS7) ───────────────────────────────────────────────────────

    public static byte[] PadTo16(byte[] data)
    {
        var pad    = 16 - (data.Length % 16);
        var padded = new byte[data.Length + pad];
        data.CopyTo(padded, 0);
        for (var i = data.Length; i < padded.Length; i++) padded[i] = (byte)pad;
        return padded;
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    /// <summary>Cryptographically secure random bytes via Baileys.Utils.Crypto.</summary>
    public static byte[] RandomBytes(int length) => Crypto.RandomBytes(length);

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        a.CopyTo(result, 0);
        b.CopyTo(result, a.Length);
        return result;
    }

    // ── Inbound ratchet helpers ────────────────────────────────────────────────

    public static byte[] DhAgreement(byte[] ourPrivate, byte[] theirPublic)
        => AuthUtils.DiffieHellman(ourPrivate, theirPublic);

    public static byte[] AdvanceChainKey(byte[] chainKey) => HmacSha256(chainKey, [0x02]);

    public static byte[] DeriveMessageKey(byte[] chainKey, int msgIndex)
    {
        var ck = chainKey;
        for (var i = 0; i < msgIndex; i++) ck = AdvanceChainKey(ck);
        var rawMsgKey = HmacSha256(ck, [0x01]);
        return Hkdf(rawMsgKey, "WhisperMessageKeys"u8.ToArray(), 48);
    }

    public static byte[] AdvanceSenderChainKey(byte[] senderChainKey) => HmacSha256(senderChainKey, [0x02]);

    public static byte[] DeriveSenderMessageKey(byte[] senderChainKey)
    {
        var rawKey = HmacSha256(senderChainKey, [0x01]);
        return Hkdf(rawKey, "WhisperGroup"u8.ToArray(), 48);
    }
}
