using System.Security.Cryptography;
using System.Text;

namespace EvolutionApi.Infrastructure.WhatsApp.Services;

public interface ISecretProtector
{
    string Protect(string value);
    string Unprotect(string protectedValue);
}

public sealed class SecretProtector(IConfiguration configuration) : ISecretProtector
{
    public string Protect(string value)
    {
        var plain = Encoding.UTF8.GetBytes(value);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(GetKey(), tag.Length)) aes.Encrypt(nonce, plain, cipher, tag);
        return "v1:" + Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    public string Unprotect(string protectedValue)
    {
        if (!protectedValue.StartsWith("v1:", StringComparison.Ordinal)) throw new CryptographicException("Unsupported encrypted secret version.");
        var packed = Convert.FromBase64String(protectedValue[3..]);
        var plain = new byte[packed.Length - 28];
        using (var aes = new AesGcm(GetKey(), 16)) aes.Decrypt(packed.AsSpan(0, 12), packed.AsSpan(28), packed.AsSpan(12, 16), plain);
        return Encoding.UTF8.GetString(plain);
    }

    private byte[] GetKey()
    {
        var key = Convert.FromBase64String(configuration["Evolution:SessionEncryptionKey"] ?? string.Empty);
        if (key.Length != 32) throw new InvalidOperationException("Evolution:SessionEncryptionKey must be a stable base64-encoded 32-byte key.");
        return key;
    }
}
