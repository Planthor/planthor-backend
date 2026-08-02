using System.Security.Cryptography;
using System.Text;

namespace Adapters.Strava.Configuration;

/// <summary>
/// Provides AES-256 encryption and decryption utilities for securing OAuth state parameters.
/// </summary>
public static class AesEncryptionHelper
{
    /// <summary>
    /// Encrypts the specified plain text using AES-256 and the provided Base64-encoded key.
    /// A random Initialization Vector (IV) is generated and prepended to the ciphertext.
    /// </summary>
    /// <param name="plainText">The string to encrypt.</param>
    /// <param name="base64Key">The 32-byte (256-bit) AES key encoded as a Base64 string.</param>
    /// <returns>A Base64-encoded string containing the IV followed by the encrypted data.</returns>
    public static string Encrypt(string plainText, string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        
        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts the specified Base64-encoded ciphertext using AES-256 and the provided Base64-encoded key.
    /// Expects the Initialization Vector (IV) to be prepended to the encrypted data.
    /// </summary>
    /// <param name="cipherText">The Base64-encoded ciphertext (IV + encrypted data).</param>
    /// <param name="base64Key">The 32-byte (256-bit) AES key encoded as a Base64 string.</param>
    /// <returns>The decrypted plain text string.</returns>
    public static string Decrypt(string cipherText, string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        var fullCipher = Convert.FromBase64String(cipherText);
        
        using var aes = Aes.Create();
        var iv = new byte[aes.BlockSize / 8];
        var cipherBytes = new byte[fullCipher.Length - iv.Length];
        
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);
        
        aes.Key = key;
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        
        return Encoding.UTF8.GetString(plainBytes);
    }
}
