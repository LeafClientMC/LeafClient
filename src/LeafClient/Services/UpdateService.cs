using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    /// <summary>
    /// Handles in-app auto-updating without a separate updater exe.
    /// Flow: check → download → stage → restart → apply on next startup.
    /// </summary>
    public static class UpdateService
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        private const string VersionUrl = "https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/latestversion.txt";
        private const string ZipUrl = "https://github.com/LeafClientMC/LeafClient/raw/refs/heads/main/latestexe/LeafClient.zip";

        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeafClient");
        private static readonly string StagingDir = Path.Combine(AppDataDir, "updates", "pending");
        private static readonly string StagingZip = Path.Combine(AppDataDir, "updates", "update.zip");
        private static readonly string StagingMarker = Path.Combine(StagingDir, ".update-ready");

        /// <summary>Latest version string found online (e.g. "1.5").</summary>
        public static string? LatestVersionString { get; private set; }

        /// <summary>Whether a staged update is ready to apply on next restart.</summary>
        public static bool IsUpdateStaged => File.Exists(StagingMarker);

        // ================================================================
        // PHASE 1 — Apply staged update at startup (call from Program.cs)
        // ================================================================

        /// <summary>
        /// Call BEFORE any Avalonia code. Applies a previously staged update
        /// by copying files over the current install directory.
        /// Returns true if an update was applied (caller should continue startup normally).
        /// </summary>
        public static bool ApplyPendingUpdate()
        {
            try
            {
                if (!File.Exists(StagingMarker))
                    return false;

                Console.WriteLine("[Updater] Found staged update, applying...");

                string appDir = AppContext.BaseDirectory;
                int copied = 0, skipped = 0;

                foreach (var file in Directory.GetFiles(StagingDir, "*", SearchOption.AllDirectories))
                {
                    // Skip the marker file
                    if (file.EndsWith(".update-ready")) continue;

                    string relative = Path.GetRelativePath(StagingDir, file);
                    string target = Path.Combine(appDir, relative);

                    try
                    {
                        string? dir = Path.GetDirectoryName(target);
                        if (dir != null) Directory.CreateDirectory(dir);
                        File.Copy(file, target, overwrite: true);
                        copied++;
                    }
                    catch (IOException)
                    {
                        // File is locked (e.g. the running exe itself) — skip, will be caught next restart
                        skipped++;
                    }
                }

                Console.WriteLine($"[Updater] Applied update: {copied} files copied, {skipped} skipped");

                // Clean up staging
                try { Directory.Delete(StagingDir, recursive: true); } catch { }
                try { File.Delete(StagingZip); } catch { }

                // If too many files were skipped, generate a fallback batch script
                if (skipped > 5)
                {
                    GenerateFallbackScript(appDir);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] Failed to apply update: {ex.Message}");
                // Clean up broken staging to prevent boot loops
                try { Directory.Delete(StagingDir, recursive: true); } catch { }
                return false;
            }
        }

        // ================================================================
        // PHASE 2 — Check for updates and download to staging
        // ================================================================

        /// <summary>
        /// Check if a new version is available. Returns the version string if update found, null otherwise.
        /// </summary>
        public static async Task<string?> CheckForUpdateAsync(CancellationToken ct = default)
        {
            try
            {
                string response = await Http.GetStringAsync(VersionUrl, ct);
                string versionText = response.Trim();

                Version? latest = null;
                if (!Version.TryParse(versionText, out latest))
                    return null;

                Version? current = Assembly.GetExecutingAssembly().GetName().Version;
                if (current == null) return null;

                if (latest > current)
                {
                    LatestVersionString = versionText;
                    Console.WriteLine($"[Updater] Update available: {current} → {latest}");
                    return versionText;
                }

                Console.WriteLine($"[Updater] Up to date (v{current})");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] Version check failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Download the update ZIP and extract to staging directory.
        /// Reports progress via callback (0.0 to 1.0).
        /// </summary>
        public static async Task<bool> DownloadAndStageAsync(
            Action<double>? onProgress = null,
            CancellationToken ct = default)
        {
            try
            {
                // Clean any previous staging
                if (Directory.Exists(StagingDir))
                    Directory.Delete(StagingDir, recursive: true);
                if (File.Exists(StagingZip))
                    File.Delete(StagingZip);

                Directory.CreateDirectory(Path.GetDirectoryName(StagingZip)!);

                Console.WriteLine("[Updater] Downloading update...");

                // Download with progress
                using var response = await Http.GetAsync(ZipUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                long downloaded = 0;

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                await using var file = File.Create(StagingZip);

                byte[] buffer = new byte[65536];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    downloaded += bytesRead;

                    if (totalBytes > 0)
                        onProgress?.Invoke((double)downloaded / totalBytes);
                }

                file.Close();

                Console.WriteLine($"[Updater] Downloaded {downloaded / 1024}KB, extracting...");

                // Extract to staging directory
                Directory.CreateDirectory(StagingDir);
                ZipFile.ExtractToDirectory(StagingZip, StagingDir, overwriteFiles: true);

                // Write marker file so next startup knows to apply
                await File.WriteAllTextAsync(StagingMarker, LatestVersionString ?? "unknown", ct);

                // Clean up zip
                try { File.Delete(StagingZip); } catch { }

                Console.WriteLine("[Updater] Update staged successfully. Will apply on next restart.");
                onProgress?.Invoke(1.0);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] Download/stage failed: {ex.Message}");
                // Clean up partial staging
                try { Directory.Delete(StagingDir, recursive: true); } catch { }
                try { File.Delete(StagingZip); } catch { }
                return false;
            }
        }

        // ================================================================
        // PHASE 3 — Restart the application
        // ================================================================

        /// <summary>
        /// Restart the application to apply the staged update.
        /// </summary>
        public static void RestartToApply()
        {
            string? exePath = Environment.ProcessPath;
            if (exePath != null)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
            Environment.Exit(0);
        }

        // ================================================================
        // FALLBACK — batch script for locked files
        // ================================================================

        private static string? GenerateFallbackScript(string appDir)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    string batPath = Path.Combine(appDir, "_apply_update.bat");
                    string exeName = Path.GetFileName(Environment.ProcessPath ?? "LeafClient.exe");
                    string script = $@"@echo off
timeout /t 2 /nobreak >nul
xcopy /s /y ""{StagingDir}\*"" ""{appDir}""
rmdir /s /q ""{StagingDir}""
start """" ""{Path.Combine(appDir, exeName)}""
del ""%~f0""
";
                    File.WriteAllText(batPath, script);
                    Console.WriteLine("[Updater] Generated fallback update script");
                    return batPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Updater] Failed to generate fallback script: {ex.Message}");
                    return null;
                }
            }
            else
            {
                try
                {
                    string shPath = Path.Combine(appDir, "_apply_update.sh");
                    string exeName = Path.GetFileName(Environment.ProcessPath ?? "LeafClient");
                    string exePath = Path.Combine(appDir, exeName);
                    string script = "#!/bin/sh\n" +
                                    "sleep 2\n" +
                                    $"cp -R \"{StagingDir}/.\" \"{appDir}\"\n" +
                                    $"rm -rf \"{StagingDir}\"\n" +
                                    $"chmod +x \"{exePath}\" 2>/dev/null\n" +
                                    $"\"{exePath}\" &\n" +
                                    "rm -- \"$0\"\n";
                    File.WriteAllText(shPath, script);
                    try
                    {
                        var chmod = new ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"+x \"{shPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        Process.Start(chmod)?.WaitForExit(2000);
                    }
                    catch { }
                    Console.WriteLine("[Updater] Generated fallback update script (shell)");
                    return shPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Updater] Failed to generate fallback script: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
