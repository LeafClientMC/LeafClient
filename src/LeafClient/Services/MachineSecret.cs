using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LeafClient.Services
{
    internal static class MachineSecret
    {
        private const string FileName = "machine_secret.bin";
        private const int SecretBytes = 32;
        private const int MinValidBytes = 16;
        private const int MaxValidBytes = 256;

        private static readonly object Lock = new();
        private static byte[]? _cached;
        private static DateTime _cachedAtUtc = DateTime.MinValue;
        private static readonly TimeSpan ReloadInterval = TimeSpan.FromSeconds(30);

        public static byte[] EnsureAndGet()
        {
            lock (Lock)
            {
                if (_cached != null && (DateTime.UtcNow - _cachedAtUtc) < ReloadInterval)
                {
                    return _cached;
                }

                var path = ResolvePath();
                if (path == null)
                {
                    throw new InvalidOperationException("Cannot resolve LeafClient app data directory for machine_secret.bin");
                }

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir!);
                }

                byte[]? loaded = TryLoad(path);
                if (loaded == null)
                {
                    loaded = Generate();
                    WriteWithOwnerOnlyAcl(path, loaded);
                    LeafLog.Info("MachineSecret", $"Generated new machine secret ({loaded.Length} bytes) at {path}");
                }

                _cached = loaded;
                _cachedAtUtc = DateTime.UtcNow;
                return loaded;
            }
        }

        public static string ComputeHmacSha256Hex(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var key = EnsureAndGet();
            using var hmac = new HMACSHA256(key);
            var sig = hmac.ComputeHash(data);
            return BytesToHex(sig);
        }

        public static byte[] CanonicalBytes(string uuid, string username, string accessToken, string type, string jwt, long ts, string nonce)
        {
            var sb = new StringBuilder(512);
            sb.Append("uuid=").Append(uuid ?? "").Append('\n');
            sb.Append("username=").Append(username ?? "").Append('\n');
            sb.Append("accessToken=").Append(accessToken ?? "").Append('\n');
            sb.Append("type=").Append(type ?? "").Append('\n');
            sb.Append("jwt=").Append(jwt ?? "").Append('\n');
            sb.Append("ts=").Append(ts).Append('\n');
            sb.Append("nonce=").Append(nonce ?? "");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string? ResolvePath()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrEmpty(appData)) return null;
                return Path.Combine(appData, "LeafClient", FileName);
            }
            catch
            {
                return null;
            }
        }

        private static byte[]? TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length < MinValidBytes || bytes.Length > MaxValidBytes)
                {
                    LeafLog.Warn("MachineSecret", $"Secret file size out of range ({bytes.Length}); regenerating.");
                    try { File.Delete(path); } catch { }
                    return null;
                }
                return bytes;
            }
            catch (Exception ex)
            {
                LeafLog.Warn("MachineSecret", $"Load failed: {ex.Message}; regenerating.");
                return null;
            }
        }

        private static byte[] Generate()
        {
            var b = new byte[SecretBytes];
            RandomNumberGenerator.Fill(b);
            return b;
        }

        private static void WriteWithOwnerOnlyAcl(string path, byte[] data)
        {
            var tmp = path + ".tmp";
            try
            {
                File.WriteAllBytes(tmp, data);
                TrySetOwnerOnlyAcl(tmp);
                if (File.Exists(path))
                {
                    try { File.Delete(path); } catch { }
                }
                File.Move(tmp, path);
                TrySetOwnerOnlyAcl(path);
            }
            catch
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                throw;
            }
        }

        private static void TrySetOwnerOnlyAcl(string path)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                {
                    try
                    {
                        var unixMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                        File.SetUnixFileMode(path, unixMode);
                    }
                    catch { }
                    return;
                }

                var fi = new FileInfo(path);
                var sec = fi.GetAccessControl();
                sec.SetAccessRuleProtection(true, false);
                var rules = sec.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                foreach (System.Security.AccessControl.FileSystemAccessRule r in rules)
                {
                    sec.RemoveAccessRuleAll(r);
                }
                var user = System.Security.Principal.WindowsIdentity.GetCurrent().User;
                if (user != null)
                {
                    sec.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                        user,
                        System.Security.AccessControl.FileSystemRights.FullControl,
                        System.Security.AccessControl.AccessControlType.Allow));
                }
                fi.SetAccessControl(sec);
            }
            catch (Exception ex)
            {
                LeafLog.Warn("MachineSecret", $"ACL tighten failed for {path}: {ex.Message}");
            }
        }

        private static string BytesToHex(byte[] b)
        {
            var sb = new StringBuilder(b.Length * 2);
            for (int i = 0; i < b.Length; i++)
            {
                sb.Append(b[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
