using System.Security.Cryptography;
using System.Text;

namespace FinanceHub.Shared.Observability.Security;

public class EnvelopeEncryptionService : IEnvelopeEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string Encrypt(string plainText, byte[] masterKey)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        if (masterKey == null || masterKey.Length != 32)
            throw new ArgumentException("A chave mestre deve possuir exatamente 256 bits (32 bytes).", nameof(masterKey));

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(masterKey, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, payload, NonceSize + TagSize, cipherBytes.Length);

        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string cipherText, byte[] masterKey)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;
        if (masterKey == null || masterKey.Length != 32)
            throw new ArgumentException("A chave mestre deve possuir exatamente 256 bits (32 bytes).", nameof(masterKey));

        var payload = Convert.FromBase64String(cipherText);
        if (payload.Length < NonceSize + TagSize)
            throw new ArgumentException("Texto cifrado inválido.", nameof(cipherText));

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[payload.Length - NonceSize - TagSize];

        Buffer.BlockCopy(payload, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(payload, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(payload, NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];
        using var aesGcm = new AesGcm(masterKey, TagSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
