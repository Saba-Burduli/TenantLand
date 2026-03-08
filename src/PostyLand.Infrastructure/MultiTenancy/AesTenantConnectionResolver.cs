using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;

namespace PostyLand.Infrastructure.MultiTenancy;

public sealed class AesTenantConnectionResolver(IOptions<EncryptionOptions> encryptionOptions)
    : ITenantConnectionResolver
{
    private readonly byte[] _key = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionOptions.Value.Key));

    public string Encrypt(string plainConnectionString)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plainConnectionString);
        var cipher = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plaintextBytes, cipher, tag);

        var payload = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, payload, nonce.Length + tag.Length, cipher.Length);
        return Convert.ToBase64String(payload);
    }

    public string ResolveDecryptedConnectionString(string encryptedConnectionString)
    {
        var payload = Convert.FromBase64String(encryptedConnectionString);
        var nonce = payload[..12];
        var tag = payload[12..28];
        var cipher = payload[28..];
        var plaintext = new byte[cipher.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}


