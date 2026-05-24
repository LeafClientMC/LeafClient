using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LeafClient.Services.ModFolderManagement
{
    public static class ModHasher
    {
        public static async Task<string?> ComputeSha256Async(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                await using var stream = File.OpenRead(filePath);
                using var sha = SHA256.Create();
                var hash = await sha.ComputeHashAsync(stream);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }

        public static string? ComputeSha256(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                using var stream = File.OpenRead(filePath);
                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(stream);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }
    }
}
