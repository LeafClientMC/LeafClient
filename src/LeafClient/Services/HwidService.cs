using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace LeafClient.Services
{
    public static class HwidService
    {
        private const string Salt = "leaf-hwid-salt-v1";

        private static string? _cachedHash;
        private static readonly object _lock = new();

        public static string GetDeviceHash()
        {
            if (_cachedHash != null) return _cachedHash;
            lock (_lock)
            {
                if (_cachedHash != null) return _cachedHash;
                _cachedHash = ComputeHash();
                return _cachedHash;
            }
        }

        private static string ComputeHash()
        {
            string machineId = SafeMachineId();
            string macAddr = SafeFirstMac();
            string raw = $"{Salt}|{machineId}|{macAddr}";
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var sb = new StringBuilder(64);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static string SafeMachineId()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return WindowsMachineGuid();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return LinuxMachineId();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return MacOsPlatformUuid();
            }
            catch { }
            return "unknown-machine";
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static string WindowsMachineGuid()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Cryptography",
                    writable: false);
                var v = key?.GetValue("MachineGuid") as string;
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            catch { }
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                    writable: false);
                var v = key?.GetValue("ProductId") as string;
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            catch { }
            return "unknown-windows";
        }

        private static string LinuxMachineId()
        {
            string[] paths = { "/etc/machine-id", "/var/lib/dbus/machine-id" };
            foreach (var path in paths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        var v = File.ReadAllText(path).Trim();
                        if (!string.IsNullOrWhiteSpace(v)) return v;
                    }
                }
                catch { }
            }
            return "unknown-linux";
        }

        private static string MacOsPlatformUuid()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/sbin/ioreg",
                    Arguments = "-rd1 -c IOPlatformExpertDevice",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                if (proc == null) return "unknown-macos";
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(2000);
                int idx = output.IndexOf("IOPlatformUUID", StringComparison.Ordinal);
                if (idx < 0) return "unknown-macos";
                int q1 = output.IndexOf('"', idx);
                if (q1 < 0) return "unknown-macos";
                int q2 = output.IndexOf('"', q1 + 1);
                int q3 = output.IndexOf('"', q2 + 1);
                int q4 = output.IndexOf('"', q3 + 1);
                if (q3 < 0 || q4 < 0) return "unknown-macos";
                return output.Substring(q3 + 1, q4 - q3 - 1);
            }
            catch { }
            return "unknown-macos";
        }

        private static string SafeFirstMac()
        {
            try
            {
                var nic = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n =>
                        n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (nic == null) return "no-mac";
                var bytes = nic.GetPhysicalAddress().GetAddressBytes();
                if (bytes.Length == 0) return "no-mac";
                return string.Join(":", bytes.Select(b => b.ToString("x2")));
            }
            catch { }
            return "no-mac";
        }
    }
}
