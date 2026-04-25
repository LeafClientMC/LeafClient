using System;
using System.Diagnostics;

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
                    Process.Start("explorer.exe", path);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", path);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", path);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SystemUtil] OpenFolder failed: {ex.Message}");
            }
        }
    }
}
