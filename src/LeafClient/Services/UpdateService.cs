using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        public enum UpdateChannel { Stable, Early }

        public static UpdateChannel CurrentChannel { get; set; } = UpdateChannel.Stable;

        private static string ChannelSubdir => CurrentChannel == UpdateChannel.Early ? "/early" : "";

        private static string VersionUrl => CurrentChannel == UpdateChannel.Early
            ? "https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/latestexe/early/latestversion.txt"
            : "https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/latestversion.txt";
        private static string ZipUrl => $"https://github.com/LeafClientMC/LeafClient/raw/refs/heads/main/latestexe{ChannelSubdir}/LeafClient.zip";
        private static string ZipSha256Url => $"https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/latestexe{ChannelSubdir}/LeafClient.zip.sha256";
        private static string ZipSignatureUrl => $"https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/latestexe{ChannelSubdir}/LeafClient.zip.sig";

        private static readonly string[] TrustedUpdateKeysB64 = new[]
        {
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEh2mUsltx9o2LZzuW6XW8uZslSUTJ+OqfAC52P4nSlcTfdetFUMcY1I2bt178Ay99r9PBGsZfZN1wDBsrwJuDmg==",
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE2NExkPJczcXU4ZfXywlYd9j47GBFqkAUIK+3zXOwxyQT/MXP7bYJernUIC8erLXKHIS3YM24rDv66zLzibDRTA==",
        };

        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeafClient");
        private static readonly string StagingDir = Path.Combine(AppDataDir, "updates", "pending");
        private static readonly string StagingZip = Path.Combine(AppDataDir, "updates", "update.zip");
        private static readonly string StagingMarker = Path.Combine(StagingDir, ".update-ready");
        private static readonly string BackupRoot = Path.Combine(AppDataDir, "updates", "backup");
        private static readonly string StateFile = Path.Combine(AppDataDir, "updates", "state.json");

        private const int MaxBootAttemptsBeforeRollback = 3;
        private static readonly TimeSpan StableBootThreshold = TimeSpan.FromSeconds(30);

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
                CheckForCrashRecovery();

                if (!File.Exists(StagingMarker))
                    return false;

                Console.WriteLine("[Updater] Found staged update, applying...");

                string appDir = AppContext.BaseDirectory;
                string previousVersion = GetCurrentAssemblyVersion();
                string newVersion = TryReadStagingMarkerVersion() ?? "unknown";

                if (!TryBackupCurrentInstall(appDir, previousVersion, out string backupPath))
                {
                    Console.WriteLine("[Updater] Backup failed — refusing to apply update.");
                    try { Directory.Delete(StagingDir, recursive: true); } catch { }
                    return false;
                }

                int copied = 0, skipped = 0;

                foreach (var file in Directory.GetFiles(StagingDir, "*", SearchOption.AllDirectories))
                {
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
                        skipped++;
                    }
                }

                Console.WriteLine($"[Updater] Applied update: {copied} files copied, {skipped} skipped");

                try { Directory.Delete(StagingDir, recursive: true); } catch { }
                try { File.Delete(StagingZip); } catch { }

                WriteState(new UpdateState
                {
                    CurrentVersion = newVersion,
                    PreviousVersion = previousVersion,
                    BackupPath = backupPath,
                    BootAttempts = 0,
                    LastStableMarkUtc = null,
                });

                if (skipped > 5)
                {
                    GenerateFallbackScript(appDir);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] Failed to apply update: {ex.Message}");
                try { Directory.Delete(StagingDir, recursive: true); } catch { }
                return false;
            }
        }

        public static void MarkBootSuccessful()
        {
            try
            {
                var state = ReadState();
                if (state == null) return;
                state.BootAttempts = 0;
                state.LastStableMarkUtc = DateTime.UtcNow;
                WriteState(state);

                if (!string.IsNullOrEmpty(state.BackupPath) && Directory.Exists(state.BackupPath))
                {
                    try
                    {
                        Directory.Delete(state.BackupPath, recursive: true);
                        Console.WriteLine($"[Updater] Marked boot stable, deleted backup at {state.BackupPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Updater] Could not delete old backup: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] MarkBootSuccessful error: {ex.Message}");
            }
        }

        private static void CheckForCrashRecovery()
        {
            try
            {
                var state = ReadState();
                if (state == null) return;

                string runningVersion = GetCurrentAssemblyVersion();
                if (!string.Equals(state.CurrentVersion, runningVersion, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                state.BootAttempts++;
                WriteState(state);

                if (state.BootAttempts >= MaxBootAttemptsBeforeRollback
                    && !string.IsNullOrEmpty(state.BackupPath)
                    && Directory.Exists(state.BackupPath))
                {
                    Console.WriteLine($"[Updater] {state.BootAttempts} consecutive boot attempts of {state.CurrentVersion} without stable mark — rolling back to {state.PreviousVersion} from {state.BackupPath}");
                    if (RestoreBackup(state.BackupPath))
                    {
                        try { Directory.Delete(state.BackupPath, recursive: true); } catch { }
                        WriteState(new UpdateState
                        {
                            CurrentVersion = state.PreviousVersion ?? "rolled-back",
                            PreviousVersion = null,
                            BackupPath = null,
                            BootAttempts = 0,
                            LastStableMarkUtc = DateTime.UtcNow,
                        });
                        Console.WriteLine("[Updater] Rollback complete. Restart to run rolled-back version.");
                        try
                        {
                            string? exePath = Environment.ProcessPath;
                            if (exePath != null)
                            {
                                Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
                            }
                        }
                        catch { }
                        Environment.Exit(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] CheckForCrashRecovery error: {ex.Message}");
            }
        }

        private static string GetCurrentAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        }

        private static string? TryReadStagingMarkerVersion()
        {
            try
            {
                if (File.Exists(StagingMarker))
                    return File.ReadAllText(StagingMarker).Trim();
            }
            catch { }
            return null;
        }

        private static bool TryBackupCurrentInstall(string appDir, string previousVersion, out string backupPath)
        {
            backupPath = "";
            try
            {
                Directory.CreateDirectory(BackupRoot);
                string folderName = $"{previousVersion}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                backupPath = Path.Combine(BackupRoot, folderName);
                Directory.CreateDirectory(backupPath);

                foreach (var file in Directory.GetFiles(appDir, "*", SearchOption.AllDirectories))
                {
                    string relative = Path.GetRelativePath(appDir, file);
                    if (relative.StartsWith("updates" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        continue;
                    string target = Path.Combine(backupPath, relative);
                    string? dir = Path.GetDirectoryName(target);
                    if (dir != null) Directory.CreateDirectory(dir);
                    try { File.Copy(file, target, overwrite: true); }
                    catch (IOException) { }
                }
                Console.WriteLine($"[Updater] Backed up current install ({previousVersion}) to {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] Backup error: {ex.Message}");
                return false;
            }
        }

        private static bool RestoreBackup(string backupPath)
        {
            try
            {
                string appDir = AppContext.BaseDirectory;
                int restored = 0;
                foreach (var file in Directory.GetFiles(backupPath, "*", SearchOption.AllDirectories))
                {
                    string relative = Path.GetRelativePath(backupPath, file);
                    string target = Path.Combine(appDir, relative);
                    string? dir = Path.GetDirectoryName(target);
                    if (dir != null) Directory.CreateDirectory(dir);
                    try { File.Copy(file, target, overwrite: true); restored++; }
                    catch (IOException) { }
                }
                Console.WriteLine($"[Updater] Restored {restored} files from backup.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] Restore error: {ex.Message}");
                return false;
            }
        }

        private static UpdateState? ReadState()
        {
            try
            {
                if (!File.Exists(StateFile)) return null;
                var json = File.ReadAllText(StateFile);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonSerializer.Deserialize(json, LeafClient.JsonContext.Default.UpdateState);
            }
            catch
            {
                return null;
            }
        }

        private static void WriteState(UpdateState state)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StateFile)!);
                var json = JsonSerializer.Serialize(state, LeafClient.JsonContext.Default.UpdateState);
                File.WriteAllText(StateFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] WriteState error: {ex.Message}");
            }
        }

        public class UpdateState
        {
            public string? CurrentVersion { get; set; }
            public string? PreviousVersion { get; set; }
            public string? BackupPath { get; set; }
            public int BootAttempts { get; set; }
            public DateTime? LastStableMarkUtc { get; set; }
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

                Console.WriteLine($"[Updater] Downloaded {downloaded / 1024}KB, verifying...");

                string? expectedHash = await TryFetchExpectedHashAsync(ct);
                if (string.IsNullOrWhiteSpace(expectedHash))
                {
                    Console.WriteLine("[Updater] Hash manifest missing or unreadable — refusing to install unsigned update.");
                    try { File.Delete(StagingZip); } catch { }
                    return false;
                }

                byte[] hashBytes = await ComputeFileSha256BytesAsync(StagingZip, ct);
                string actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[Updater] Hash mismatch (expected {expectedHash}, got {actualHash}). Discarding update.");
                    try { File.Delete(StagingZip); } catch { }
                    return false;
                }

                byte[]? signature = await TryFetchSignatureAsync(ct);
                if (signature is null)
                {
                    Console.WriteLine("[Updater] Signature missing or unreadable — refusing to install unsigned update.");
                    try { File.Delete(StagingZip); } catch { }
                    return false;
                }

                if (!VerifySignature(hashBytes, signature))
                {
                    Console.WriteLine("[Updater] Signature verification FAILED. Discarding update.");
                    try { File.Delete(StagingZip); } catch { }
                    return false;
                }

                Console.WriteLine($"[Updater] Hash + ECDSA signature verified ({actualHash[..16]}…), extracting...");

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

        private static async Task<string?> TryFetchExpectedHashAsync(CancellationToken ct)
        {
            try
            {
                using var resp = await Http.GetAsync(ZipSha256Url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var raw = (await resp.Content.ReadAsStringAsync(ct)).Trim();
                if (string.IsNullOrWhiteSpace(raw)) return null;
                var firstToken = raw.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (firstToken.Length == 0) return null;
                var hex = firstToken[0];
                if (hex.Length != 64) return null;
                foreach (var c in hex)
                {
                    if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                        return null;
                }
                return hex.ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }

        private static async Task<byte[]> ComputeFileSha256BytesAsync(string path, CancellationToken ct)
        {
            await using var fs = File.OpenRead(path);
            using var sha = System.Security.Cryptography.SHA256.Create();
            return await sha.ComputeHashAsync(fs, ct);
        }

        private static async Task<byte[]?> TryFetchSignatureAsync(CancellationToken ct)
        {
            try
            {
                using var resp = await Http.GetAsync(ZipSignatureUrl, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var raw = (await resp.Content.ReadAsStringAsync(ct)).Trim();
                if (string.IsNullOrWhiteSpace(raw)) return null;
                try { return Convert.FromBase64String(raw); }
                catch { return null; }
            }
            catch
            {
                return null;
            }
        }

        private static bool VerifySignature(byte[] sha256Hash, byte[] signature)
        {
            foreach (var keyB64 in TrustedUpdateKeysB64)
            {
                try
                {
                    using var ecdsa = System.Security.Cryptography.ECDsa.Create();
                    if (ecdsa is null) continue;
                    ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(keyB64), out _);
                    if (ecdsa.VerifyHash(sha256Hash, signature)) return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Updater] Signature verify error against one trusted key: {ex.Message}");
                }
            }
            return false;
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
