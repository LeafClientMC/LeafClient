using System;
using System.Security.Cryptography;
using System.Text;

namespace LeafClient.Services
{
    public static class EncryptionService
    {
        // Use a more complex key derivation
        private static readonly byte[] _salt = Encoding.UTF8.GetBytes("LeafClient2024Salt");
        private static readonly string _passphrase = "LeafClientSecureKey123!@#";

        public static string Encrypt(string plaintext)
        {
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(_passphrase, _salt, 10000, HashAlgorithmName.SHA256);
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        public static string Decrypt(string ciphertext)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    var key = new Rfc2898DeriveBytes(_passphrase, _salt, 10000, HashAlgorithmName.SHA256);
                    aes.Key = key.GetBytes(32);
                    aes.IV = key.GetBytes(16);

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(ciphertext);
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EncryptionService ERROR] Decryption failed: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
