using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Text;

namespace LeafClient.Services
{
    public static class AutostartService
    {
        private const string AppName = "LeafClient";
        private const string AppDisplayName = "Leaf Client";
        private const string AppDescription = "Leaf Client Minecraft launcher";

        public static bool TrySetEnabled(bool enable)
        {
            try
            {
                if (OperatingSystem.IsWindows()) return SetEnabledWindows(enable);
                if (OperatingSystem.IsMacOS()) return SetEnabledMac(enable);
                if (OperatingSystem.IsLinux()) return SetEnabledLinux(enable);
                return false;
            }
            catch (Exception ex)
            {
                LeafLog.Error("Autostart", $"Set enabled={enable} failed: {ex.Message}");
                return false;
            }
        }

        public static bool IsEnabled()
        {
            try
            {
                if (OperatingSystem.IsWindows()) return IsEnabledWindows();
                if (OperatingSystem.IsMacOS()) return IsEnabledMac();
                if (OperatingSystem.IsLinux()) return IsEnabledLinux();
                return false;
            }
            catch { return false; }
        }

        private static string GetExecutablePath()
        {
            string? path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) return path;
            path = Process.GetCurrentProcess().MainModule?.FileName;
            return path ?? "";
        }

        [SupportedOSPlatform("windows")]
        private static class WinReg
        {
            private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

            public static bool Set(bool enable, string exePath)
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                if (key == null) return false;
                if (enable)
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    if (key.GetValue(AppName) != null) key.DeleteValue(AppName);
                }
                return true;
            }

            public static bool IsSet()
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
                return key?.GetValue(AppName) != null;
            }
        }

        [SupportedOSPlatform("windows")]
        private static bool SetEnabledWindows(bool enable)
        {
            string exe = GetExecutablePath();
            if (string.IsNullOrEmpty(exe)) return false;
            return WinReg.Set(enable, exe);
        }

        [SupportedOSPlatform("windows")]
        private static bool IsEnabledWindows() => WinReg.IsSet();

        private static string MacPlistPath
        {
            get
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, "Library", "LaunchAgents", "com.leafclient.launcher.plist");
            }
        }

        private static bool SetEnabledMac(bool enable)
        {
            string plistPath = MacPlistPath;
            if (!enable)
            {
                if (File.Exists(plistPath))
                {
                    try { Process.Start(new ProcessStartInfo("launchctl", $"unload \"{plistPath}\"") { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(2000); } catch { }
                    File.Delete(plistPath);
                }
                return true;
            }

            string exe = GetExecutablePath();
            if (string.IsNullOrEmpty(exe)) return false;

            string dir = Path.GetDirectoryName(plistPath) ?? "";
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string plist =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
                "<plist version=\"1.0\">\n" +
                "<dict>\n" +
                "    <key>Label</key>\n" +
                "    <string>com.leafclient.launcher</string>\n" +
                "    <key>ProgramArguments</key>\n" +
                "    <array>\n" +
                $"        <string>{System.Security.SecurityElement.Escape(exe)}</string>\n" +
                "    </array>\n" +
                "    <key>RunAtLoad</key>\n" +
                "    <true/>\n" +
                "    <key>KeepAlive</key>\n" +
                "    <false/>\n" +
                "    <key>ProcessType</key>\n" +
                "    <string>Interactive</string>\n" +
                "</dict>\n" +
                "</plist>\n";

            File.WriteAllText(plistPath, plist, new UTF8Encoding(false));
            try { Process.Start(new ProcessStartInfo("launchctl", $"load \"{plistPath}\"") { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(2000); } catch { }
            return true;
        }

        private static bool IsEnabledMac() => File.Exists(MacPlistPath);

        private static string LinuxDesktopPath
        {
            get
            {
                string xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? "";
                if (string.IsNullOrEmpty(xdg))
                {
                    string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    xdg = Path.Combine(home, ".config");
                }
                return Path.Combine(xdg, "autostart", "leafclient.desktop");
            }
        }

        private static bool SetEnabledLinux(bool enable)
        {
            string path = LinuxDesktopPath;
            if (!enable)
            {
                if (File.Exists(path)) File.Delete(path);
                return true;
            }

            string exe = GetExecutablePath();
            if (string.IsNullOrEmpty(exe)) return false;

            string dir = Path.GetDirectoryName(path) ?? "";
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string escapedExe = exe.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("`", "\\`").Replace("$", "\\$");
            string entry =
                "[Desktop Entry]\n" +
                "Type=Application\n" +
                $"Name={AppDisplayName}\n" +
                $"Comment={AppDescription}\n" +
                $"Exec=\"{escapedExe}\"\n" +
                "X-GNOME-Autostart-enabled=true\n" +
                "Hidden=false\n" +
                "NoDisplay=false\n" +
                "Terminal=false\n";

            File.WriteAllText(path, entry, new UTF8Encoding(false));
            return true;
        }

        private static bool IsEnabledLinux() => File.Exists(LinuxDesktopPath);
    }
}
