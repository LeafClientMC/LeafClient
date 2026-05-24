#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8625
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace LeafClient.Services
{
    public enum GpuVendor
    {
        Unknown,
        Nvidia,
        Amd,
        Intel,
        Apple
    }

    public enum GpuType
    {
        Unknown,
        Integrated,
        Discrete
    }

    public sealed class GpuInfo
    {
        public string Name { get; set; } = "Unknown GPU";
        public GpuVendor Vendor { get; set; } = GpuVendor.Unknown;
        public GpuType Type { get; set; } = GpuType.Unknown;
    }

    public static class GpuDetectionService
    {
        public static GpuInfo Detect()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return DetectWindows();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return DetectMacOS();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return DetectLinux();
            }
            catch (Exception ex)
            {
                LeafLog.Info("GpuDetection", $"Detection failed: {ex.Message}");
            }
            return new GpuInfo();
        }

        public static string Recommend(GpuInfo gpu)
        {
            if (gpu.Vendor == GpuVendor.Apple) return "sodium";
            if (gpu.Vendor == GpuVendor.Intel && gpu.Type != GpuType.Discrete) return "sodium";
            if (gpu.Type == GpuType.Integrated) return "sodium";
            if (gpu.Vendor == GpuVendor.Nvidia && gpu.Type == GpuType.Discrete) return "vulkanmod";
            if (gpu.Vendor == GpuVendor.Amd && gpu.Type == GpuType.Discrete) return "vulkanmod";
            return "sodium";
        }

        private static GpuInfo DetectWindows()
        {
            var info = new GpuInfo();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -Command \"Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return info;
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(5000);

                string best = PickBestGpuName(output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                if (!string.IsNullOrEmpty(best))
                {
                    info.Name = best.Trim();
                    info.Vendor = VendorFromName(info.Name);
                    info.Type = TypeFromName(info.Name, info.Vendor);
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("GpuDetection", $"Win PowerShell path failed: {ex.Message}");
            }
            return info;
        }

        private static GpuInfo DetectLinux()
        {
            var info = new GpuInfo();
            try
            {
                for (int i = 0; i < 8; i++)
                {
                    string vendorPath = $"/sys/class/drm/card{i}/device/vendor";
                    if (!File.Exists(vendorPath)) continue;
                    string vendorHex = File.ReadAllText(vendorPath).Trim().ToLowerInvariant();
                    GpuVendor v = vendorHex switch
                    {
                        "0x10de" => GpuVendor.Nvidia,
                        "0x1002" => GpuVendor.Amd,
                        "0x8086" => GpuVendor.Intel,
                        _ => GpuVendor.Unknown
                    };
                    if (v == GpuVendor.Unknown) continue;
                    info.Vendor = v;
                    info.Type = v == GpuVendor.Intel ? GpuType.Integrated : GpuType.Discrete;
                    if (v == GpuVendor.Nvidia || v == GpuVendor.Amd) break;
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("GpuDetection", $"Linux /sys path failed: {ex.Message}");
            }

            if (info.Vendor == GpuVendor.Unknown)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "/bin/sh",
                        Arguments = "-c \"lspci -nn | grep -iE 'vga|3d|display'\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit(5000);
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            string line = lines[0];
                            info.Name = line;
                            info.Vendor = VendorFromName(line);
                            info.Type = TypeFromName(line, info.Vendor);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LeafLog.Info("GpuDetection", $"Linux lspci fallback failed: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(info.Name) || info.Name == "Unknown GPU")
            {
                info.Name = info.Vendor switch
                {
                    GpuVendor.Nvidia => "Nvidia GPU",
                    GpuVendor.Amd => "AMD GPU",
                    GpuVendor.Intel => "Intel GPU",
                    _ => "Unknown GPU"
                };
            }
            return info;
        }

        private static GpuInfo DetectMacOS()
        {
            var info = new GpuInfo();
            bool appleSilicon = false;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/sbin/sysctl",
                    Arguments = "-n hw.optional.arm64",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string val = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit(3000);
                    appleSilicon = val == "1";
                }
            }
            catch { }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/sbin/system_profiler",
                    Arguments = "SPDisplaysDataType",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(8000);
                    foreach (var raw in output.Split('\n'))
                    {
                        string line = raw.Trim();
                        if (line.StartsWith("Chipset Model:", StringComparison.OrdinalIgnoreCase))
                        {
                            string name = line.Substring("Chipset Model:".Length).Trim();
                            if (!string.IsNullOrEmpty(name))
                            {
                                info.Name = name;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("GpuDetection", $"macOS system_profiler failed: {ex.Message}");
            }

            if (appleSilicon || info.Name.Contains("Apple", StringComparison.OrdinalIgnoreCase)
                             || info.Name.StartsWith("M1", StringComparison.OrdinalIgnoreCase)
                             || info.Name.StartsWith("M2", StringComparison.OrdinalIgnoreCase)
                             || info.Name.StartsWith("M3", StringComparison.OrdinalIgnoreCase)
                             || info.Name.StartsWith("M4", StringComparison.OrdinalIgnoreCase))
            {
                info.Vendor = GpuVendor.Apple;
                info.Type = GpuType.Integrated;
                if (string.IsNullOrEmpty(info.Name) || info.Name == "Unknown GPU") info.Name = "Apple GPU";
            }
            else
            {
                info.Vendor = VendorFromName(info.Name);
                info.Type = TypeFromName(info.Name, info.Vendor);
            }
            return info;
        }

        private static string PickBestGpuName(string[] candidates)
        {
            string fallback = "";
            foreach (var raw in candidates)
            {
                string s = raw?.Trim() ?? "";
                if (string.IsNullOrEmpty(s)) continue;
                if (s.Contains("Microsoft Basic", StringComparison.OrdinalIgnoreCase)) continue;
                if (s.Contains("Remote Display", StringComparison.OrdinalIgnoreCase)) continue;
                if (s.Contains("ParSec", StringComparison.OrdinalIgnoreCase)) continue;
                if (s.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase)) continue;
                var v = VendorFromName(s);
                if (v == GpuVendor.Nvidia || v == GpuVendor.Amd) return s;
                if (string.IsNullOrEmpty(fallback)) fallback = s;
            }
            return fallback;
        }

        private static GpuVendor VendorFromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return GpuVendor.Unknown;
            string n = name.ToLowerInvariant();
            if (n.Contains("nvidia") || n.Contains("geforce") || n.Contains("quadro") || n.Contains("rtx") || n.Contains("gtx") || n.Contains("tesla")) return GpuVendor.Nvidia;
            if (n.Contains("amd") || n.Contains("radeon") || n.Contains("ati ") || n.Contains("rx ") || n.Contains("vega")) return GpuVendor.Amd;
            if (n.Contains("intel") || n.Contains("uhd") || n.Contains("hd graphics") || n.Contains("iris") || n.Contains("arc ")) return GpuVendor.Intel;
            if (n.Contains("apple") || n.StartsWith("m1") || n.StartsWith("m2") || n.StartsWith("m3") || n.StartsWith("m4")) return GpuVendor.Apple;
            return GpuVendor.Unknown;
        }

        private static GpuType TypeFromName(string name, GpuVendor vendor)
        {
            if (string.IsNullOrEmpty(name)) return GpuType.Unknown;
            string n = name.ToLowerInvariant();
            if (vendor == GpuVendor.Intel)
            {
                if (n.Contains("arc a")) return GpuType.Discrete;
                return GpuType.Integrated;
            }
            if (vendor == GpuVendor.Apple) return GpuType.Integrated;
            if (vendor == GpuVendor.Nvidia) return GpuType.Discrete;
            if (vendor == GpuVendor.Amd)
            {
                if (n.Contains("vega") && (n.Contains("ryzen") || n.Contains("apu") || n.Contains("graphics"))) return GpuType.Integrated;
                if (n.Contains("radeon graphics") && !n.Contains("rx ")) return GpuType.Integrated;
                return GpuType.Discrete;
            }
            return GpuType.Unknown;
        }
    }
}
