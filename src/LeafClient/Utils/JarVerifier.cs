using System;
using System.Security.Cryptography;

namespace LeafClient.Utils
{
    public static class JarVerifier
    {
        public static bool VerifySha256(byte[] fileBytes, string expectedHex)
        {
            if (fileBytes is null) throw new ArgumentNullException(nameof(fileBytes));
            if (string.IsNullOrWhiteSpace(expectedHex)) return false;

            var expected = TryDecodeHex(expectedHex.Trim());
            if (expected is null) return false;
            if (expected.Length != 32) return false;

            var actual = SHA256.HashData(fileBytes);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        public static bool IsAllowedHost(Uri url, string expectedHost)
        {
            if (url is null) return false;
            if (string.IsNullOrWhiteSpace(expectedHost)) return false;
            if (!url.IsAbsoluteUri) return false;
            if (!string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)) return false;
            return string.Equals(url.Host, expectedHost, StringComparison.OrdinalIgnoreCase);
        }

        private static byte[]? TryDecodeHex(string hex)
        {
            if (hex.Length % 2 != 0) return null;
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int hi = DecodeNibble(hex[i * 2]);
                int lo = DecodeNibble(hex[i * 2 + 1]);
                if (hi < 0 || lo < 0) return null;
                bytes[i] = (byte)((hi << 4) | lo);
            }
            return bytes;
        }

        private static int DecodeNibble(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }
    }
}
