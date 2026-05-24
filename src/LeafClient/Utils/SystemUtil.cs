using System;
using System.Diagnostics;
using System.IO;
using LeafClient.Services;

namespace LeafClient.Utils
{
    public static class SystemUtil
    {
        public static double GetMemoryMb()
        {
            return 8192;
        }

        public static void OpenFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start("explorer.exe", $"\"{path}\"");
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", new[] { path });
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", new[] { path });
                }
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("SystemUtil", $"OpenFolder failed: {ex.Message}");
            }
        }

        public static void OpenFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", new[] { path });
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", new[] { path });
                }
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("SystemUtil", $"OpenFile failed: {ex.Message}");
            }
        }

        public static void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", new[] { url });
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", new[] { url });
                }
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("SystemUtil", $"OpenUrl failed: {ex.Message}");
            }
        }
    }
}
