using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public static class UpdateService
    {
        private static readonly HttpClient Http = new HttpClient(CertificatePinning.CreateHandler()) { Timeout = TimeSpan.FromSeconds(30) };

        public enum UpdateChannel { Stable, Early }

        public static UpdateChannel CurrentChannel { get; set; } = UpdateChannel.Stable;

        private static string ChannelSubdir => CurrentChannel == UpdateChannel.Early ? "/early" : "";

        private const string CdnBase = "https://cdn.leafclient.com/updates";

        public static string GetArtifactFilename()
        {
            if (OperatingSystem.IsWindows())
                return "LeafClient-win-x64.zip";

            if (OperatingSystem.IsLinux())
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPIMAGE")))
                    return "LeafClient-linux-x86_64.AppImage";
                return "LeafClient-linux-x64.tar.gz";
            }

            if (OperatingSystem.IsMacOS())
            {
                return RuntimeInformation.OSArchitecture == Architecture.Arm64
                    ? "LeafClient-mac-arm64.dmg"
                    : "LeafClient-mac-x64.dmg";
            }

            return "LeafClient-win-x64.zip";
        }

        private static string ArtifactUrl => $"{CdnBase}{ChannelSubdir}/{GetArtifactFilename()}";
        private static string VersionUrl => $"{CdnBase}{ChannelSubdir}/latestversion.txt";
        private static string ZipUrl => ArtifactUrl;
        private static string ZipSha256Url => $"{ArtifactUrl}.sha256";
        private static string ZipSignatureUrl => $"{ArtifactUrl}.sig";

        private static readonly string[] TrustedUpdateKeysB64 = new[]
        {
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEh2mUsltx9o2LZzuW6XW8uZslSUTJ+OqfAC52P4nSlcTfdetFUMcY1I2bt178Ay99r9PBGsZfZN1wDBsrwJuDmg==",
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE2NExkPJczcXU4ZfXywlYd9j47GBFqkAUIK+3zXOwxyQT/MXP7bYJernUIC8erLXKHIS3YM24rDv66zLzibDRTA==",
        };

        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeafClient");
        private static readonly string StagingDir = Path.Combine(AppDataDir, "updates", "pending");
        private static readonly string StagingArchivesDir = Path.Combine(AppDataDir, "updates");
        private static string StagingZip => Path.Combine(StagingArchivesDir, GetArtifactFilename());
        private static readonly string StagingMarker = Path.Combine(StagingDir, ".update-ready");
        private static readonly string BackupRoot = Path.Combine(AppDataDir, "updates", "backup");
        private static readonly string StateFile = Path.Combine(AppDataDir, "updates", "state.json");

        private const int MaxBootAttemptsBeforeRollback = 3;
        private static readonly TimeSpan StableBootThreshold = TimeSpan.FromSeconds(30);

        public static string? LatestVersionString { get; private set; }

        public static bool IsUpdateStaged => File.Exists(StagingMarker);

        public static bool ApplyPendingUpdate()
        {
            try
            {
                CheckForCrashRecovery();

                if (!File.Exists(StagingMarker))
                    return false;

                LeafLog.Info("Updater", "Found staged update, applying...");

                string appDir = AppContext.BaseDirectory;
                string previousVersion = GetCurrentAssemblyVersion();
                string newVersion = TryReadStagingMarkerVersion() ?? "unknown";

                if (TryApplyAppImageUpdate(previousVersion, newVersion))
                    return true;

                string copyDestination = appDir;
                if (OperatingSystem.IsMacOS())
                {
                    string? stagedApp = Directory.EnumerateDirectories(StagingDir, "*.app").FirstOrDefault();
                    if (stagedApp != null)
                    {
                        string? currentAppRoot = FindMacAppBundleRoot(appDir);
                        if (currentAppRoot != null)
                        {
                            HoistMacAppBundleContents(stagedApp);
                            copyDestination = currentAppRoot;
                            LeafLog.Info("Updater", $"macOS .app mode: target = {currentAppRoot}");
                        }
                    }
                }

                if (!TryBackupCurrentInstall(copyDestination, previousVersion, out string backupPath))
                {
                    LeafLog.Error("Updater", "Backup failed - refusing to apply update.");
                    try { Directory.Delete(StagingDir, recursive: true); } catch { }
                    return false;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return ApplyPendingUpdateWindows(copyDestination, previousVersion, newVersion, backupPath);
                }

                int copied = 0, skipped = 0;

                foreach (var file in Directory.GetFiles(StagingDir, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".update-ready")) continue;

                    string relative = Path.GetRelativePath(StagingDir, file);
                    string target = Path.Combine(copyDestination, relative);

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

                LeafLog.Info("Updater", $"Applied update: {copied} files copied, {skipped} skipped");

                ApplyPlatformPostApplyFixes(copyDestination);
                appDir = copyDestination;

                if (!SanityCheckInstall(appDir))
                {
                    LeafLog.Error("Updater", "Post-apply sanity check FAILED - rolling back from backup.");
                    if (RestoreBackup(backupPath))
                    {
                        try { Directory.Delete(StagingDir, recursive: true); } catch { }
                        try { File.Delete(StagingZip); } catch { }
                        WriteState(new UpdateState
                        {
                            CurrentVersion = previousVersion,
                            PreviousVersion = null,
                            BackupPath = null,
                            BootAttempts = 0,
                            LastStableMarkUtc = DateTime.UtcNow,
                        });
                        return false;
                    }
                }

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

                RelaunchAfterUpdateUnix(appDir);
                return true;
            }
            catch (Exception ex)
            {
                LeafLog.Error("Updater", $"Failed to apply update: {ex.Message}");
                try { Directory.Delete(StagingDir, recursive: true); } catch { }
                return false;
            }
        }

        private static void RelaunchAfterUpdateUnix(string appDir)
        {
            try
            {
                string logPath = Path.Combine(AppDataDir, "relaunch.log");
                try { Directory.CreateDirectory(AppDataDir); } catch { }

                if (OperatingSystem.IsMacOS())
                {
                    string? appRoot = FindMacAppBundleRoot(appDir);
                    if (appRoot != null)
                    {
                        SpawnDetachedShell($"sleep 3 && /usr/bin/open -n \"{appRoot}\" >> \"{logPath}\" 2>&1", logPath);
                        LeafLog.Info("Updater", $"macOS: scheduled detached relaunch of {appRoot}");
                        Environment.Exit(0);
                        return;
                    }
                }

                string? exePath = Path.Combine(appDir, "LeafClient");
                if (!File.Exists(exePath))
                {
                    LeafLog.Info("Updater", "Unix: could not locate updated executable, skipping relaunch");
                    return;
                }

                try { File.SetUnixFileMode(exePath, File.GetUnixFileMode(exePath) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute); } catch { }

                string esc = exePath.Replace("\"", "\\\"");
                string cmd = $"sleep 3 && LEAFCLIENT_POST_UPDATE_RELAUNCH=1 setsid \"{esc}\" </dev/null >> \"{logPath}\" 2>&1 < /dev/null &";
                SpawnDetachedShell(cmd, logPath);
                LeafLog.Info("Updater", $"Unix: scheduled detached relaunch of {exePath}");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"Unix relaunch failed: {ex.Message}");
            }
        }

        private static void SpawnDetachedShell(string command, string logPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c '{command.Replace("'", "'\\''")}'",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                };
                try { File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] spawn: {command}\n"); } catch { }
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(logPath, $"[{DateTime.UtcNow:O}] spawn-failed: {ex.Message}\n"); } catch { }
                throw;
            }
        }

        private static bool ApplyPendingUpdateWindows(string copyDestination, string previousVersion, string newVersion, string backupPath)
        {
            try
            {
                string? scriptPath = GenerateWindowsApplyScript(copyDestination);
                if (scriptPath == null)
                {
                    LeafLog.Error("Updater", "Windows: failed to generate apply script.");
                    return false;
                }

                WriteState(new UpdateState
                {
                    CurrentVersion = newVersion,
                    PreviousVersion = previousVersion,
                    BackupPath = backupPath,
                    BootAttempts = 0,
                    LastStableMarkUtc = null,
                });

                LeafLog.Info("Updater", $"Windows: spawning apply script {scriptPath}");
                Process.Start(new ProcessStartInfo
                {
                    FileName = scriptPath,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                });

                Environment.Exit(0);
                return true;
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"Windows apply failed: {ex.Message}");
                return false;
            }
        }

        private static string? GenerateWindowsApplyScript(string appDir)
        {
            try
            {
                string scriptDir = AppDataDir;
                Directory.CreateDirectory(scriptDir);
                string batPath = Path.Combine(scriptDir, "_apply_update.bat");
                string logPath = Path.Combine(scriptDir, "apply.log");
                string exeName = Path.GetFileName(Environment.ProcessPath ?? "LeafClient.exe");
                string exePath = Path.Combine(appDir, exeName);
                string script = "@echo off\r\n"
                    + "setlocal enabledelayedexpansion\r\n"
                    + $"echo [%DATE% %TIME%] apply-start staging=\"{StagingDir}\" target=\"{appDir}\" exe=\"{exePath}\" >> \"{logPath}\"\r\n"
                    + "timeout /t 3 /nobreak >nul\r\n"
                    + "set retry=0\r\n"
                    + ":copy_loop\r\n"
                    + $"xcopy /s /e /y /h /r \"{StagingDir}\\*\" \"{appDir}\\\" >> \"{logPath}\" 2>&1\r\n"
                    + "if errorlevel 1 (\r\n"
                    + "  set /a retry+=1\r\n"
                    + "  if !retry! lss 5 (\r\n"
                    + "    timeout /t 1 /nobreak >nul\r\n"
                    + "    goto copy_loop\r\n"
                    + "  )\r\n"
                    + ")\r\n"
                    + $"del /q \"{StagingMarker}\" >nul 2>&1\r\n"
                    + $"rmdir /s /q \"{StagingDir}\" >nul 2>&1\r\n"
                    + $"del /q \"{StagingZip}\" >nul 2>&1\r\n"
                    + "set LEAFCLIENT_POST_UPDATE_RELAUNCH=1\r\n"
                    + $"if not exist \"{exePath}\" (\r\n"
                    + $"  echo [%DATE% %TIME%] relaunch-fail exe-missing >> \"{logPath}\"\r\n"
                    + "  goto done\r\n"
                    + ")\r\n"
                    + $"echo [%DATE% %TIME%] relaunching via explorer >> \"{logPath}\"\r\n"
                    + $"%SystemRoot%\\explorer.exe \"{exePath}\"\r\n"
                    + "timeout /t 2 /nobreak >nul\r\n"
                    + $"tasklist /FI \"IMAGENAME eq {exeName}\" 2>nul | find /i \"{exeName}\" >nul\r\n"
                    + "if errorlevel 1 (\r\n"
                    + $"  echo [%DATE% %TIME%] explorer relaunch failed, trying start >> \"{logPath}\"\r\n"
                    + $"  start \"\" \"{exePath}\"\r\n"
                    + "  timeout /t 2 /nobreak >nul\r\n"
                    + $"  tasklist /FI \"IMAGENAME eq {exeName}\" 2>nul | find /i \"{exeName}\" >nul\r\n"
                    + "  if errorlevel 1 (\r\n"
                    + $"    echo [%DATE% %TIME%] start failed, direct exec >> \"{logPath}\"\r\n"
                    + $"    \"{exePath}\"\r\n"
                    + "  )\r\n"
                    + ")\r\n"
                    + ":done\r\n"
                    + $"echo [%DATE% %TIME%] apply-done >> \"{logPath}\"\r\n"
                    + "endlocal\r\n"
                    + "(goto) 2>nul & del \"%~f0\"\r\n";
                File.WriteAllText(batPath, script);
                return batPath;
            }
            catch (Exception ex)
            {
                LeafLog.Error("Updater", $"Failed to write Windows apply script: {ex.Message}");
                return null;
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
                state.FailedVersion = null;
                state.FailedAtUtc = null;
                WriteState(state);

                if (!string.IsNullOrEmpty(state.BackupPath) && Directory.Exists(state.BackupPath))
                {
                    try
                    {
                        Directory.Delete(state.BackupPath, recursive: true);
                        LeafLog.Info("Updater", $"Marked boot stable, deleted backup at {state.BackupPath}");
                    }
                    catch (Exception ex)
                    {
                        LeafLog.Info("Updater", $"Could not delete old backup: {ex.Message}");
                    }
                }

                PruneStaleBackups(TimeSpan.FromDays(30));
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"MarkBootSuccessful error: {ex.Message}");
            }
        }

        private static void PruneStaleBackups(TimeSpan maxAge)
        {
            try
            {
                if (!Directory.Exists(BackupRoot)) return;
                DateTime cutoff = DateTime.UtcNow - maxAge;
                foreach (var dir in Directory.EnumerateDirectories(BackupRoot))
                {
                    try
                    {
                        if (Directory.GetCreationTimeUtc(dir) < cutoff)
                        {
                            Directory.Delete(dir, recursive: true);
                            LeafLog.Info("Updater", $"Pruned stale backup: {dir}");
                        }
                    }
                    catch { }
                }
            }
            catch { }
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
                    if (!string.IsNullOrEmpty(state.CurrentVersion)
                        && !string.Equals(state.CurrentVersion, state.PreviousVersion, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(state.CurrentVersion, "rolled-back", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(state.CurrentVersion, "unknown", StringComparison.OrdinalIgnoreCase))
                    {
                        LeafLog.Error("Updater", $"Version mismatch: state claims {state.CurrentVersion} but running {runningVersion}. Marking {state.CurrentVersion} as failed to prevent download loop.");
                        state.FailedVersion = state.CurrentVersion;
                        state.FailedAtUtc = DateTime.UtcNow;
                        state.CurrentVersion = runningVersion;
                        state.BootAttempts = 0;
                        WriteState(state);
                    }
                    return;
                }

                state.BootAttempts++;
                WriteState(state);

                if (state.BootAttempts >= MaxBootAttemptsBeforeRollback
                    && !string.IsNullOrEmpty(state.BackupPath)
                    && Directory.Exists(state.BackupPath))
                {
                    LeafLog.Info("Updater", $"{state.BootAttempts} consecutive boot attempts of {state.CurrentVersion} without stable mark - rolling back to {state.PreviousVersion} from {state.BackupPath}");
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
                        LeafLog.Info("Updater", "Rollback complete. Restart to run rolled-back version.");
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
                LeafLog.Info("Updater", $"CheckForCrashRecovery error: {ex.Message}");
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
                LeafLog.Info("Updater", $"Backed up current install ({previousVersion}) to {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"Backup error: {ex.Message}");
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
                LeafLog.Info("Updater", $"Restored {restored} files from backup.");
                return true;
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"Restore error: {ex.Message}");
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
                LeafLog.Info("Updater", $"WriteState error: {ex.Message}");
            }
        }

        public class UpdateState
        {
            public string? CurrentVersion { get; set; }
            public string? PreviousVersion { get; set; }
            public string? BackupPath { get; set; }
            public int BootAttempts { get; set; }
            public DateTime? LastStableMarkUtc { get; set; }
            public string? FailedVersion { get; set; }
            public DateTime? FailedAtUtc { get; set; }
        }

        public enum UpdateCheckOutcome
        {
            UpToDate,
            UpdateAvailable,
            VersionFileMissing,
            NetworkError,
        }

        public sealed record UpdateCheckResult(UpdateCheckOutcome Outcome, string? Version);

        public static async Task<string?> CheckForUpdateAsync(CancellationToken ct = default)
        {
            var r = await CheckForUpdateDetailedAsync(ct);
            return r.Outcome == UpdateCheckOutcome.UpdateAvailable ? r.Version : null;
        }

        public static async Task<UpdateCheckResult> CheckForUpdateDetailedAsync(CancellationToken ct = default)
        {
            try
            {
                var (response, status) = await HttpGetStringDetailedAsync(VersionUrl, ct);
                if (response == null)
                {
                    if (status == 404)
                    {
                        LeafLog.Info("Updater", $"Version file missing at {VersionUrl} (HTTP 404).");
                        return new UpdateCheckResult(UpdateCheckOutcome.VersionFileMissing, null);
                    }
                    return new UpdateCheckResult(UpdateCheckOutcome.NetworkError, null);
                }

                string versionText = response.Trim();

                if (!Version.TryParse(versionText, out Version? latest))
                {
                    LeafLog.Info("Updater", $"Version file unparseable: '{versionText}'");
                    return new UpdateCheckResult(UpdateCheckOutcome.VersionFileMissing, null);
                }

                Version? current = Assembly.GetExecutingAssembly().GetName().Version;
                if (current == null)
                    return new UpdateCheckResult(UpdateCheckOutcome.NetworkError, null);

                if (latest > current)
                {
                    var prior = ReadState();
                    if (prior != null
                        && !string.IsNullOrEmpty(prior.FailedVersion)
                        && string.Equals(prior.FailedVersion, versionText, StringComparison.OrdinalIgnoreCase)
                        && prior.FailedAtUtc.HasValue
                        && (DateTime.UtcNow - prior.FailedAtUtc.Value) < TimeSpan.FromHours(24))
                    {
                        LeafLog.Error("Updater", $"Update {versionText} previously failed to apply at {prior.FailedAtUtc:O} - skipping for 24h to avoid loop.");
                        return new UpdateCheckResult(UpdateCheckOutcome.UpToDate, versionText);
                    }
                    LatestVersionString = versionText;
                    LeafLog.Info("Updater", $"Update available: {current} → {latest}");
                    return new UpdateCheckResult(UpdateCheckOutcome.UpdateAvailable, versionText);
                }

                LeafLog.Info("Updater", $"Up to date (v{current})");
                return new UpdateCheckResult(UpdateCheckOutcome.UpToDate, versionText);
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"Version check failed: {ex.Message}");
                return new UpdateCheckResult(UpdateCheckOutcome.NetworkError, null);
            }
        }

        public static async Task<bool> DownloadAndStageAsync(
            Action<double>? onProgress = null,
            CancellationToken ct = default)
        {
            try
            {
                if (Directory.Exists(StagingDir))
                    Directory.Delete(StagingDir, recursive: true);
                if (File.Exists(StagingZip))
                    File.Delete(StagingZip);

                Directory.CreateDirectory(Path.GetDirectoryName(StagingZip)!);

                LeafLog.Info("Updater", "Downloading update...");

                HttpResponseMessage? response = null;
                Exception? lastErr = null;
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        response = await Http.GetAsync(ZipUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                        if (response.IsSuccessStatusCode) break;
                        int code = (int)response.StatusCode;
                        LeafLog.Info("Updater", $"Zip GET attempt {attempt} returned {code}");
                        response.Dispose();
                        response = null;
                        if (code >= 400 && code < 500)
                        {
                            LeafLog.Info("Updater", $"Zip GET {code} - artifact missing, not retrying.");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        lastErr = ex;
                        LeafLog.Info("Updater", $"Zip GET attempt {attempt} failed: {ex.Message}");
                    }
                    if (attempt < 3)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                }
                if (response == null)
                {
                    LeafLog.Error("Updater", $"Zip download failed after 3 attempts. Last error: {lastErr?.Message ?? "non-success status"}");
                    return false;
                }
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

                LeafLog.Info("Updater", $"Downloaded {downloaded / 1024}KB, verifying...");

                string? expectedHash = await TryFetchExpectedHashAsync(ct);
                if (string.IsNullOrWhiteSpace(expectedHash))
                {
                    LeafLog.Info("Updater", "Hash manifest missing or unreadable - refusing to install unsigned update.");
                    try { File.Delete(StagingZip); } catch { }
                    return false;
                }

                byte[] hashBytes = await ComputeFileSha256BytesAsync(StagingZip, ct);
                string actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    LeafLog.Info("Updater", $"Hash mismatch (expected {expectedHash}, got {actualHash}). Discarding update.");
                    try { File.Delete(StagingZip); } catch { }
                    return false;
                }

                byte[]? signature = await TryFetchSignatureAsync(ct);
                if (signature is null)
                {
                    LeafLog.Info("Updater", "Signature missing or unreadable - refusing to install unsigned update.");
                    try { File.Delete(StagingZip); } catch { }
                    return false;
                }

                if (!VerifySignature(hashBytes, signature))
                {
                    LeafLog.Info("Updater", "Signature verification FAILED. Discarding update.");
                    try { File.Delete(StagingZip); } catch { }
                    return false;
                }

                LeafLog.Info("Updater", $"Hash + ECDSA signature verified ({actualHash[..16]}…), extracting...");

                Directory.CreateDirectory(StagingDir);
                bool extracted = await ExtractArchiveToStagingAsync(StagingZip, StagingDir, ct);
                if (!extracted)
                {
                    LeafLog.Info("Updater", "Archive extraction failed.");
                    try { Directory.Delete(StagingDir, recursive: true); } catch { }
                    try { File.Delete(StagingZip); } catch { }
                    return false;
                }

                if (!SanityCheckInstall(StagingDir))
                {
                    LeafLog.Error("Updater", "Staged content failed sanity check - discarding.");
                    try { Directory.Delete(StagingDir, recursive: true); } catch { }
                    try { File.Delete(StagingZip); } catch { }
                    return false;
                }

                await File.WriteAllTextAsync(StagingMarker, LatestVersionString ?? "unknown", ct);

                try { File.Delete(StagingZip); } catch { }

                LeafLog.Info("Updater", "Update staged successfully. Will apply on next restart.");
                onProgress?.Invoke(1.0);
                return true;
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"Download/stage failed: {ex.Message}");
                try { Directory.Delete(StagingDir, recursive: true); } catch { }
                try { File.Delete(StagingZip); } catch { }
                return false;
            }
        }

        private static async Task<string?> TryFetchExpectedHashAsync(CancellationToken ct)
        {
            string? raw = await HttpGetStringWithRetryAsync(ZipSha256Url, ct);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            try
            {
                var firstToken = raw.Trim().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
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

        private static async Task<string?> HttpGetStringWithRetryAsync(string url, CancellationToken ct)
        {
            var (body, _) = await HttpGetStringDetailedAsync(url, ct);
            return body;
        }

        private static async Task<(string? body, int status)> HttpGetStringDetailedAsync(string url, CancellationToken ct)
        {
            Exception? lastErr = null;
            int lastStatus = 0;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using var resp = await Http.GetAsync(url, ct);
                    lastStatus = (int)resp.StatusCode;
                    if (resp.IsSuccessStatusCode)
                        return (await resp.Content.ReadAsStringAsync(ct), lastStatus);

                    if (lastStatus >= 400 && lastStatus < 500)
                    {
                        LeafLog.Info("Updater", $"GET {url} returned {lastStatus} - not retrying.");
                        return (null, lastStatus);
                    }
                    LeafLog.Info("Updater", $"GET {url} attempt {attempt} returned {lastStatus}");
                }
                catch (Exception ex)
                {
                    lastErr = ex;
                    LeafLog.Info("Updater", $"GET {url} attempt {attempt} failed: {ex.Message}");
                }
                if (attempt < 3)
                {
                    try { await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct); }
                    catch (OperationCanceledException) { return (null, lastStatus); }
                }
            }
            if (lastErr != null)
                LeafLog.Info("Updater", $"GET {url} gave up after 3 attempts. Last error: {lastErr.Message}");
            return (null, lastStatus);
        }

        private static async Task<byte[]> ComputeFileSha256BytesAsync(string path, CancellationToken ct)
        {
            await using var fs = File.OpenRead(path);
            using var sha = System.Security.Cryptography.SHA256.Create();
            return await sha.ComputeHashAsync(fs, ct);
        }

        private static async Task<byte[]?> TryFetchSignatureAsync(CancellationToken ct)
        {
            string? raw = await HttpGetStringWithRetryAsync(ZipSignatureUrl, ct);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            try { return Convert.FromBase64String(raw.Trim()); }
            catch { return null; }
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
                    LeafLog.Error("Updater", $"Signature verify error against one trusted key: {ex.Message}");
                }
            }
            return false;
        }

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

        private static async Task<bool> ExtractArchiveToStagingAsync(string archivePath, string stagingDir, CancellationToken ct)
        {
            string name = Path.GetFileName(archivePath).ToLowerInvariant();

            try
            {
                if (name.EndsWith(".zip"))
                {
                    ZipFile.ExtractToDirectory(archivePath, stagingDir, overwriteFiles: true);
                    return true;
                }

                if (name.EndsWith(".tar.gz") || name.EndsWith(".tgz"))
                {
                    return await ExtractTarGzAsync(archivePath, stagingDir, ct);
                }

                if (name.EndsWith(".appimage"))
                {
                    Directory.CreateDirectory(stagingDir);
                    string target = Path.Combine(stagingDir, "LeafClient.AppImage");
                    if (File.Exists(target)) File.Delete(target);
                    File.Move(archivePath, target);
                    return true;
                }

                if (name.EndsWith(".dmg"))
                {
                    return ExtractDmgToStaging(archivePath, stagingDir);
                }

                LeafLog.Info("Updater", $"Unknown archive format: {name}");
                return false;
            }
            catch (Exception ex)
            {
                LeafLog.Error("Updater", $"Extraction failed ({name}): {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ExtractTarGzAsync(string tarGzPath, string stagingDir, CancellationToken ct)
        {
            try
            {
                Directory.CreateDirectory(stagingDir);
                await using var fileStream = File.OpenRead(tarGzPath);
                await using var gzStream = new GZipStream(fileStream, CompressionMode.Decompress);
                await System.Formats.Tar.TarFile.ExtractToDirectoryAsync(
                    gzStream, stagingDir, overwriteFiles: true, ct);
                return true;
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"tar.gz extract failed: {ex.Message}");
                return false;
            }
        }

        private static bool ExtractDmgToStaging(string dmgPath, string stagingDir)
        {
            string mountPoint = $"/tmp/leafclient-update-{Guid.NewGuid():N}";
            try
            {
                Directory.CreateDirectory(mountPoint);
                Directory.CreateDirectory(stagingDir);

                int attachCode = RunCommandSilent("hdiutil",
                    $"attach -nobrowse -readonly -mountpoint \"{mountPoint}\" \"{dmgPath}\"",
                    timeoutMs: 30000);
                if (attachCode != 0)
                {
                    LeafLog.Error("Updater", $"hdiutil attach failed (code {attachCode}).");
                    return false;
                }

                bool ok = false;
                try
                {
                    string? appPath = Directory.EnumerateDirectories(mountPoint, "*.app").FirstOrDefault();
                    if (appPath == null)
                    {
                        LeafLog.Info("Updater", "No .app bundle found inside DMG.");
                        return false;
                    }

                    string destApp = Path.Combine(stagingDir, Path.GetFileName(appPath));
                    CopyDirectoryRecursive(appPath, destApp);
                    ok = true;
                    return true;
                }
                finally
                {
                    RunCommandSilent("hdiutil", $"detach \"{mountPoint}\" -force", timeoutMs: 15000);
                    try { Directory.Delete(mountPoint, recursive: true); } catch { }
                    if (!ok)
                    {
                        try { Directory.Delete(stagingDir, recursive: true); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"DMG extract failed: {ex.Message}");
                return false;
            }
        }

        private static void CopyDirectoryRecursive(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(source, dir);
                Directory.CreateDirectory(Path.Combine(destination, relative));
            }
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(source, file);
                string target = Path.Combine(destination, relative);
                string? targetDir = Path.GetDirectoryName(target);
                if (targetDir != null) Directory.CreateDirectory(targetDir);
                File.Copy(file, target, overwrite: true);
            }
        }

        private static void ApplyPlatformPostApplyFixes(string appDir)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            try
            {
                var execMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                             | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                             | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

                int chmodCount = 0;
                foreach (var file in Directory.EnumerateFiles(appDir, "*", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(file);
                    string lower = name.ToLowerInvariant();

                    bool needsExec =
                        name.Equals("LeafClient", StringComparison.Ordinal) ||
                        name.Equals("LeafClientUpdater", StringComparison.Ordinal) ||
                        name.Equals("LeafClientInstaller", StringComparison.Ordinal) ||
                        lower.EndsWith(".sh", StringComparison.Ordinal) ||
                        lower.EndsWith(".so", StringComparison.Ordinal) ||
                        lower.Contains(".so.", StringComparison.Ordinal) ||
                        lower.EndsWith(".dylib", StringComparison.Ordinal) ||
                        file.Contains("/Contents/MacOS/", StringComparison.Ordinal);

                    if (!needsExec) continue;

                    try
                    {
                        File.SetUnixFileMode(file, execMode);
                        chmodCount++;
                    }
                    catch (Exception ex)
                    {
                        LeafLog.Error("Updater", $"chmod failed for {file}: {ex.Message}");
                    }
                }
                LeafLog.Info("Updater", $"Applied exec bit to {chmodCount} files.");
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"ApplyPlatformPostApplyFixes (chmod) error: {ex.Message}");
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;

            try
            {
                string? bundleRoot = FindMacAppBundleRoot(appDir);
                if (bundleRoot == null)
                {
                    LeafLog.Info("Updater", "macOS: no .app bundle found, applying xattr+codesign to appDir directly.");
                    bundleRoot = appDir;
                }
                else
                {
                    LeafLog.Info("Updater", $"macOS: found .app bundle at {bundleRoot}");
                }

                RunCommandSilent("xattr", $"-dr com.apple.quarantine \"{bundleRoot}\"", timeoutMs: 8000);
                RunCommandSilent("codesign", $"--force --deep --sign - --timestamp=none \"{bundleRoot}\"", timeoutMs: 15000);
                LeafLog.Info("Updater", "macOS: quarantine stripped and ad-hoc re-signed.");
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"macOS post-apply (xattr/codesign) error: {ex.Message}");
            }
        }

        private static bool TryApplyAppImageUpdate(string previousVersion, string newVersion)
        {
            string stagedAppImage = Path.Combine(StagingDir, "LeafClient.AppImage");
            if (!OperatingSystem.IsLinux() || !File.Exists(stagedAppImage))
                return false;

            string? appimagePath = Environment.GetEnvironmentVariable("APPIMAGE");
            if (string.IsNullOrEmpty(appimagePath))
            {
                LeafLog.Info("Updater", "AppImage staged but APPIMAGE env not set. Falling back to file-copy mode.");
                return false;
            }

            try
            {
                LeafLog.Info("Updater", $"AppImage mode: replacing {appimagePath}");

                string backupAppImage = appimagePath + ".bak";
                try
                {
                    if (File.Exists(backupAppImage)) File.Delete(backupAppImage);
                    if (File.Exists(appimagePath)) File.Copy(appimagePath, backupAppImage, overwrite: true);
                }
                catch (Exception ex)
                {
                    LeafLog.Info("Updater", $"AppImage backup failed: {ex.Message} - refusing to proceed.");
                    return false;
                }

                try
                {
                    File.Move(stagedAppImage, appimagePath, overwrite: true);
                    var execMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                                 | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                                 | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
                    File.SetUnixFileMode(appimagePath, execMode);
                }
                catch (Exception ex)
                {
                    LeafLog.Info("Updater", $"AppImage replace failed: {ex.Message} - attempting restore from backup.");
                    try { File.Copy(backupAppImage, appimagePath, overwrite: true); } catch { }
                    return false;
                }

                try { Directory.Delete(StagingDir, recursive: true); } catch { }
                try { File.Delete(StagingZip); } catch { }

                WriteState(new UpdateState
                {
                    CurrentVersion = newVersion,
                    PreviousVersion = previousVersion,
                    BackupPath = backupAppImage,
                    BootAttempts = 0,
                    LastStableMarkUtc = null,
                });

                LeafLog.Info("Updater", "AppImage updated successfully.");
                return true;
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"AppImage update unexpected error: {ex.Message}");
                return false;
            }
        }

        private static void HoistMacAppBundleContents(string stagedAppBundle)
        {
            try
            {
                string parent = Path.GetDirectoryName(stagedAppBundle)!;
                foreach (var entry in Directory.EnumerateFileSystemEntries(stagedAppBundle).ToArray())
                {
                    string newPath = Path.Combine(parent, Path.GetFileName(entry));
                    if (Directory.Exists(entry))
                    {
                        if (Directory.Exists(newPath)) Directory.Delete(newPath, recursive: true);
                        Directory.Move(entry, newPath);
                    }
                    else
                    {
                        if (File.Exists(newPath)) File.Delete(newPath);
                        File.Move(entry, newPath);
                    }
                }
                try { Directory.Delete(stagedAppBundle, recursive: true); } catch { }
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"Mac .app hoist failed: {ex.Message}");
            }
        }

        private static string? FindMacAppBundleRoot(string appDir)
        {
            try
            {
                var current = new DirectoryInfo(appDir);
                for (int i = 0; i < 6 && current != null; i++)
                {
                    if (current.Name.EndsWith(".app", StringComparison.Ordinal))
                        return current.FullName;
                    current = current.Parent;
                }
            }
            catch { }
            return null;
        }

        private static bool SanityCheckInstall(string appDir)
        {
            try
            {
                string binaryName = "LeafClient";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    binaryName = "LeafClient.exe";

                string expected = Path.Combine(appDir, binaryName);
                if (File.Exists(expected))
                {
                    var len = new FileInfo(expected).Length;
                    if (len < 1024)
                    {
                        LeafLog.Info("Updater", $"Sanity check: {expected} too small ({len} bytes).");
                        return false;
                    }
                    return true;
                }

                foreach (var f in Directory.EnumerateFiles(appDir, binaryName, SearchOption.AllDirectories))
                {
                    var len = new FileInfo(f).Length;
                    if (len >= 1024) return true;
                }

                LeafLog.Info("Updater", $"Sanity check: launcher binary '{binaryName}' not found in {appDir}.");
                return false;
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"Sanity check error: {ex.Message}");
                return false;
            }
        }

        private static int RunCommandSilent(string fileName, string arguments, int timeoutMs)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using var p = Process.Start(psi);
                if (p == null) return -1;
                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(true); } catch { }
                    return -2;
                }
                return p.ExitCode;
            }
            catch (Exception ex)
            {
                LeafLog.Info("Updater", $"RunCommandSilent '{fileName}' failed: {ex.Message}");
                return -3;
            }
        }

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
                    LeafLog.Info("Updater", "Generated fallback update script");
                    return batPath;
                }
                catch (Exception ex)
                {
                    LeafLog.Error("Updater", $"Failed to generate fallback script: {ex.Message}");
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
                    LeafLog.Info("Updater", "Generated fallback update script (shell)");
                    return shPath;
                }
                catch (Exception ex)
                {
                    LeafLog.Error("Updater", $"Failed to generate fallback script: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
