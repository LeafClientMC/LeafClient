using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace LeafClient.Services
{
    public static class CredentialProtection
    {
        private static readonly byte[] Pepper = Encoding.UTF8.GetBytes("LeafClient/v1/launcher-secrets");

        public static byte[] Protect(byte[] plain)
        {
            if (plain is null) throw new ArgumentNullException(nameof(plain));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return WindowsDpapi.Protect(plain);
            }
            return AesGcmFallback.Encrypt(plain);
        }

        public static byte[] Unprotect(byte[] cipher)
        {
            if (cipher is null) throw new ArgumentNullException(nameof(cipher));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return WindowsDpapi.Unprotect(cipher);
            }
            return AesGcmFallback.Decrypt(cipher);
        }

        public static string ProtectString(string plain)
        {
            return Convert.ToBase64String(Protect(Encoding.UTF8.GetBytes(plain)));
        }

        public static string UnprotectString(string cipherB64)
        {
            return Encoding.UTF8.GetString(Unprotect(Convert.FromBase64String(cipherB64)));
        }

        private static class WindowsDpapi
        {
            private const uint CRYPTPROTECT_UI_FORBIDDEN = 0x1;

            [StructLayout(LayoutKind.Sequential)]
            private struct DATA_BLOB
            {
                public int cbData;
                public IntPtr pbData;
            }

            [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern bool CryptProtectData(
                ref DATA_BLOB pDataIn,
                string? szDataDescr,
                IntPtr pOptionalEntropy,
                IntPtr pvReserved,
                IntPtr pPromptStruct,
                uint dwFlags,
                ref DATA_BLOB pDataOut);

            [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern bool CryptUnprotectData(
                ref DATA_BLOB pDataIn,
                IntPtr ppszDataDescr,
                IntPtr pOptionalEntropy,
                IntPtr pvReserved,
                IntPtr pPromptStruct,
                uint dwFlags,
                ref DATA_BLOB pDataOut);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern IntPtr LocalFree(IntPtr hMem);

            public static byte[] Protect(byte[] plain)
            {
                var inBlob = new DATA_BLOB();
                var outBlob = new DATA_BLOB();
                IntPtr inPtr = Marshal.AllocHGlobal(plain.Length);
                try
                {
                    Marshal.Copy(plain, 0, inPtr, plain.Length);
                    inBlob.cbData = plain.Length;
                    inBlob.pbData = inPtr;
                    if (!CryptProtectData(ref inBlob, "LeafClient", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                            CRYPTPROTECT_UI_FORBIDDEN, ref outBlob))
                    {
                        throw new CryptographicException("CryptProtectData failed (Win32 error " + Marshal.GetLastWin32Error() + ")");
                    }
                    var result = new byte[outBlob.cbData];
                    Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
                    return result;
                }
                finally
                {
                    Marshal.FreeHGlobal(inPtr);
                    if (outBlob.pbData != IntPtr.Zero) LocalFree(outBlob.pbData);
                }
            }

            public static byte[] Unprotect(byte[] cipher)
            {
                var inBlob = new DATA_BLOB();
                var outBlob = new DATA_BLOB();
                IntPtr inPtr = Marshal.AllocHGlobal(cipher.Length);
                try
                {
                    Marshal.Copy(cipher, 0, inPtr, cipher.Length);
                    inBlob.cbData = cipher.Length;
                    inBlob.pbData = inPtr;
                    if (!CryptUnprotectData(ref inBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                            CRYPTPROTECT_UI_FORBIDDEN, ref outBlob))
                    {
                        throw new CryptographicException("CryptUnprotectData failed (Win32 error " + Marshal.GetLastWin32Error() + ")");
                    }
                    var result = new byte[outBlob.cbData];
                    Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
                    return result;
                }
                finally
                {
                    Marshal.FreeHGlobal(inPtr);
                    if (outBlob.pbData != IntPtr.Zero) LocalFree(outBlob.pbData);
                }
            }
        }

        private static class AesGcmFallback
        {
            private const int NonceSize = 12;
            private const int TagSize = 16;
            private const int Version = 1;

            private static byte[] DeriveKey()
            {
                string machineId = ReadMachineId() ?? Environment.MachineName + "|" + Environment.UserName;
                using var pbkdf2 = new Rfc2898DeriveBytes(
                    Encoding.UTF8.GetBytes(machineId), Pepper, 100_000, HashAlgorithmName.SHA256);
                return pbkdf2.GetBytes(32);
            }

            private static string? ReadMachineId()
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo("/usr/sbin/ioreg", "-rd1 -c IOPlatformExpertDevice")
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        };
                        using var p = System.Diagnostics.Process.Start(psi);
                        if (p == null) return null;
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(2000);
                        var idx = output.IndexOf("IOPlatformUUID", StringComparison.Ordinal);
                        if (idx < 0) return null;
                        var quoteStart = output.IndexOf('"', idx + 14);
                        if (quoteStart < 0) return null;
                        var valStart = output.IndexOf('"', quoteStart + 1);
                        if (valStart < 0) return null;
                        var valEnd = output.IndexOf('"', valStart + 1);
                        if (valEnd < 0) return null;
                        return output.Substring(valStart + 1, valEnd - valStart - 1);
                    }
                    if (File.Exists("/etc/machine-id"))
                    {
                        return File.ReadAllText("/etc/machine-id").Trim();
                    }
                    if (File.Exists("/var/lib/dbus/machine-id"))
                    {
                        return File.ReadAllText("/var/lib/dbus/machine-id").Trim();
                    }
                }
                catch { }
                return null;
            }

            public static byte[] Encrypt(byte[] plain)
            {
                byte[] key = DeriveKey();
                byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
                byte[] ciphertext = new byte[plain.Length];
                byte[] tag = new byte[TagSize];
                using (var aes = new AesGcm(key, TagSize))
                {
                    aes.Encrypt(nonce, plain, ciphertext, tag);
                }
                Array.Clear(key, 0, key.Length);

                byte[] result = new byte[1 + NonceSize + TagSize + ciphertext.Length];
                result[0] = (byte)Version;
                Buffer.BlockCopy(nonce, 0, result, 1, NonceSize);
                Buffer.BlockCopy(tag, 0, result, 1 + NonceSize, TagSize);
                Buffer.BlockCopy(ciphertext, 0, result, 1 + NonceSize + TagSize, ciphertext.Length);
                return result;
            }

            public static byte[] Decrypt(byte[] cipher)
            {
                if (cipher.Length < 1 + NonceSize + TagSize)
                    throw new CryptographicException("Cipher too short");
                if (cipher[0] != Version)
                    throw new CryptographicException("Unsupported credential version: " + cipher[0]);

                byte[] nonce = new byte[NonceSize];
                byte[] tag = new byte[TagSize];
                byte[] ciphertext = new byte[cipher.Length - 1 - NonceSize - TagSize];
                Buffer.BlockCopy(cipher, 1, nonce, 0, NonceSize);
                Buffer.BlockCopy(cipher, 1 + NonceSize, tag, 0, TagSize);
                Buffer.BlockCopy(cipher, 1 + NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

                byte[] key = DeriveKey();
                byte[] plain = new byte[ciphertext.Length];
                try
                {
                    using var aes = new AesGcm(key, TagSize);
                    aes.Decrypt(nonce, ciphertext, tag, plain);
                    return plain;
                }
                finally
                {
                    Array.Clear(key, 0, key.Length);
                }
            }
        }
    }
}
