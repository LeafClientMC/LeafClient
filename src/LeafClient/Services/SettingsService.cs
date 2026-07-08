using LeafClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public class SettingsService
    {
        private static readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly string _settingsFilePath;
        private readonly string _secretsFilePath;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDirectory = Path.Combine(appDataPath, "LeafClient");
            Directory.CreateDirectory(appDirectory);
            _settingsFilePath = Path.Combine(appDirectory, "settings.json");
            _secretsFilePath = Path.Combine(appDirectory, "secrets.dat");
        }

        public async Task SaveSettingsAsync(LauncherSettings settings)
        {
            await _fileLock.WaitAsync();
            try
            {
                var secrets = ExtractSecrets(settings);
                WriteSecretsFile(secrets);

                var jsonWithSecrets = JsonSerializer.Serialize(settings, JsonContext.Default.LauncherSettings);
                var clone = JsonSerializer.Deserialize(jsonWithSecrets, JsonContext.Default.LauncherSettings);
                if (clone == null) return;
                StripSecretsInPlace(clone);
                var jsonString = JsonSerializer.Serialize(clone, JsonContext.Default.LauncherSettings);
                await AtomicWriteAllTextAsync(_settingsFilePath, jsonString);
                LeafLog.Info("Settings", "Settings saved successfully.");
            }
            catch (Exception ex)
            {
                LeafLog.Error("Settings", $"Error saving settings: {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private static async Task WriteWithFsyncAsync(string path, byte[] bytes)
        {
            using var fs = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.WriteThrough);
            await fs.WriteAsync(bytes, 0, bytes.Length);
            await fs.FlushAsync();
            try { fs.Flush(flushToDisk: true); } catch { }
        }

        private static async Task AtomicWriteAllTextAsync(string targetPath, string content)
        {
            var tmpPath = targetPath + ".tmp";
            var bakPath = targetPath + ".bak";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            await WriteWithFsyncAsync(tmpPath, bytes);
            if (new FileInfo(tmpPath).Length <= 0)
            {
                try { File.Delete(tmpPath); } catch { }
                throw new IOException($"Refusing to commit zero-byte write to {targetPath}");
            }
            try
            {
                if (File.Exists(targetPath))
                {
                    var existingBytes = new FileInfo(targetPath).Length;
                    if (existingBytes > 0)
                    {
                        try { File.Copy(targetPath, bakPath, overwrite: true); } catch { }
                    }
                }
            }
            catch { }
            File.Move(tmpPath, targetPath, overwrite: true);
        }

        public async Task<LauncherSettings> LoadSettingsAsync()
        {
            var bakPath = _settingsFilePath + ".bak";
            if (!File.Exists(_settingsFilePath) && !File.Exists(bakPath))
            {
                LeafLog.Info("Settings", "Settings file not found. Loading default settings.");
                return new LauncherSettings();
            }

            await _fileLock.WaitAsync();
            try
            {
                var jsonString = await TryReadGoodSettingsAsync(_settingsFilePath, bakPath);
                if (jsonString == null)
                {
                    LeafLog.Info("Settings", "Both primary and backup are unreadable. Loading defaults.");
                    return new LauncherSettings();
                }
                LauncherSettings? settings = null;
                try
                {
                    settings = JsonSerializer.Deserialize(jsonString, JsonContext.Default.LauncherSettings);
                }
                catch (JsonException jex)
                {
                    LeafLog.Error("Settings", $"Primary deserialize failed ({jex.Message}); attempting backup.");
                    if (File.Exists(bakPath))
                    {
                        try
                        {
                            var bakJson = await File.ReadAllTextAsync(bakPath);
                            if (!string.IsNullOrWhiteSpace(bakJson))
                            {
                                settings = JsonSerializer.Deserialize(bakJson, JsonContext.Default.LauncherSettings);
                                if (settings != null)
                                {
                                    LeafLog.Info("Settings", "Recovered from .bak.");
                                    try { File.Copy(bakPath, _settingsFilePath, overwrite: true); } catch { }
                                }
                            }
                        }
                        catch (Exception bex)
                        {
                            LeafLog.Info("Settings", $"Backup also unreadable: {bex.Message}");
                        }
                    }
                }

                if (settings != null)
                {
                    settings.SelectedAddonByVersion ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    settings = new LauncherSettings();
                }

                bool needsMigration = HasPlaintextSecrets(settings);
                var secrets = ReadSecretsFile();
                if (secrets != null)
                {
                    MergeSecrets(settings, secrets);
                }

                if (needsMigration)
                {
                    LeafLog.Info("Secrets", "Plaintext secrets detected - migrating to encrypted secrets.dat.");
                    var migrated = ExtractSecrets(settings);
                    try
                    {
                        WriteSecretsFile(migrated);
                        var snapshots = StripSecretsInPlace(settings);
                        try
                        {
                            var migrationJson = JsonSerializer.Serialize(settings, JsonContext.Default.LauncherSettings);
                            await File.WriteAllTextAsync(_settingsFilePath, migrationJson);
                        }
                        finally
                        {
                            RestoreSecretsInPlace(settings, snapshots);
                        }
                    }
                    catch (Exception ex)
                    {
                        LeafLog.Info("Secrets", $"Migration write failed: {ex.Message}");
                    }
                }

                MigrateProfileModSchema(settings);

                LeafLog.Info("Settings", "Settings loaded successfully.");
                return settings;
            }
            catch (JsonException ex)
            {
                LeafLog.Error("Settings", $"Error deserializing settings file: {ex.Message}. Loading default settings.");
                return new LauncherSettings();
            }
            catch (Exception ex)
            {
                LeafLog.Error("Settings", $"Error loading settings: {ex.Message}. Loading default settings.");
                return new LauncherSettings();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private static void MigrateProfileModSchema(LauncherSettings settings)
        {
            if (settings?.Profiles == null) return;
            string globalEngine = !string.IsNullOrEmpty(settings.RenderBackendChoice)
                ? settings.RenderBackendChoice.ToLowerInvariant()
                : (settings.IsVulkanModEnabled ? "vulkanmod" : "sodium");
            foreach (var p in settings.Profiles)
            {
                if (p == null) continue;
                if (string.IsNullOrEmpty(p.RenderEngine))
                    p.RenderEngine = globalEngine;
                if (p.CoreModOverrides == null)
                    p.CoreModOverrides = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                if (p.DisabledRequiredMods != null && p.DisabledRequiredMods.Count > 0)
                {
                    foreach (var kv in p.DisabledRequiredMods)
                    {
                        if (kv.Value && !p.CoreModOverrides.ContainsKey(kv.Key))
                            p.CoreModOverrides[kv.Key] = false;
                    }
                    p.DisabledRequiredMods.Clear();
                }
            }
        }

        private static bool HasPlaintextSecrets(LauncherSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.SessionAccessToken)) return true;
            if (!string.IsNullOrEmpty(settings.MicrosoftRefreshToken)) return true;
            if (!string.IsNullOrEmpty(settings.LeafApiJwt)) return true;
            if (!string.IsNullOrEmpty(settings.LeafApiRefreshToken)) return true;
            if (!string.IsNullOrEmpty(settings.SessionXuid)) return true;
            if (settings.SavedAccounts != null)
            {
                foreach (var acct in settings.SavedAccounts)
                {
                    if (acct == null) continue;
                    if (!string.IsNullOrEmpty(acct.AccessToken)) return true;
                    if (!string.IsNullOrEmpty(acct.LeafApiJwt)) return true;
                    if (!string.IsNullOrEmpty(acct.LeafApiRefreshToken)) return true;
                    if (!string.IsNullOrEmpty(acct.Xuid)) return true;
                }
            }
            return false;
        }

        private static SettingsSecrets ExtractSecrets(LauncherSettings settings)
        {
            var secrets = new SettingsSecrets
            {
                SessionAccessToken = settings.SessionAccessToken,
                SessionXuid = settings.SessionXuid,
                MicrosoftRefreshToken = settings.MicrosoftRefreshToken,
                LeafApiJwt = settings.LeafApiJwt,
                LeafApiRefreshToken = settings.LeafApiRefreshToken,
            };
            if (settings.SavedAccounts != null)
            {
                foreach (var acct in settings.SavedAccounts)
                {
                    if (acct == null || string.IsNullOrEmpty(acct.Id)) continue;
                    secrets.Accounts[acct.Id] = new AccountSecrets
                    {
                        AccessToken = acct.AccessToken,
                        Xuid = acct.Xuid,
                        LeafApiJwt = acct.LeafApiJwt,
                        LeafApiRefreshToken = acct.LeafApiRefreshToken,
                    };
                }
            }
            return secrets;
        }

        private readonly struct SecretSnapshot
        {
            public readonly string? SessionAccessToken;
            public readonly string? SessionXuid;
            public readonly string? MicrosoftRefreshToken;
            public readonly string? LeafApiJwt;
            public readonly string? LeafApiRefreshToken;
            public readonly Dictionary<string, AccountSecrets> Accounts;

            public SecretSnapshot(LauncherSettings s)
            {
                SessionAccessToken = s.SessionAccessToken;
                SessionXuid = s.SessionXuid;
                MicrosoftRefreshToken = s.MicrosoftRefreshToken;
                LeafApiJwt = s.LeafApiJwt;
                LeafApiRefreshToken = s.LeafApiRefreshToken;
                Accounts = new Dictionary<string, AccountSecrets>();
                if (s.SavedAccounts != null)
                {
                    foreach (var acct in s.SavedAccounts)
                    {
                        if (acct == null || string.IsNullOrEmpty(acct.Id)) continue;
                        Accounts[acct.Id] = new AccountSecrets
                        {
                            AccessToken = acct.AccessToken,
                            Xuid = acct.Xuid,
                            LeafApiJwt = acct.LeafApiJwt,
                            LeafApiRefreshToken = acct.LeafApiRefreshToken,
                        };
                    }
                }
            }
        }

        private static SecretSnapshot StripSecretsInPlace(LauncherSettings settings)
        {
            var snap = new SecretSnapshot(settings);
            settings.SessionAccessToken = null;
            settings.SessionXuid = null;
            settings.MicrosoftRefreshToken = null;
            settings.LeafApiJwt = null;
            settings.LeafApiRefreshToken = null;
            if (settings.SavedAccounts != null)
            {
                foreach (var acct in settings.SavedAccounts)
                {
                    if (acct == null) continue;
                    acct.AccessToken = null;
                    acct.Xuid = null;
                    acct.LeafApiJwt = null;
                    acct.LeafApiRefreshToken = null;
                }
            }
            return snap;
        }

        private static void RestoreSecretsInPlace(LauncherSettings settings, SecretSnapshot snap)
        {
            settings.SessionAccessToken = snap.SessionAccessToken;
            settings.SessionXuid = snap.SessionXuid;
            settings.MicrosoftRefreshToken = snap.MicrosoftRefreshToken;
            settings.LeafApiJwt = snap.LeafApiJwt;
            settings.LeafApiRefreshToken = snap.LeafApiRefreshToken;
            if (settings.SavedAccounts != null)
            {
                foreach (var acct in settings.SavedAccounts)
                {
                    if (acct == null || string.IsNullOrEmpty(acct.Id)) continue;
                    if (snap.Accounts.TryGetValue(acct.Id, out var s))
                    {
                        acct.AccessToken = s.AccessToken;
                        acct.Xuid = s.Xuid;
                        acct.LeafApiJwt = s.LeafApiJwt;
                        acct.LeafApiRefreshToken = s.LeafApiRefreshToken;
                    }
                }
            }
        }

        private static void MergeSecrets(LauncherSettings settings, SettingsSecrets secrets)
        {
            if (!string.IsNullOrEmpty(secrets.SessionAccessToken)) settings.SessionAccessToken = secrets.SessionAccessToken;
            if (!string.IsNullOrEmpty(secrets.SessionXuid)) settings.SessionXuid = secrets.SessionXuid;
            if (!string.IsNullOrEmpty(secrets.MicrosoftRefreshToken)) settings.MicrosoftRefreshToken = secrets.MicrosoftRefreshToken;
            if (!string.IsNullOrEmpty(secrets.LeafApiJwt)) settings.LeafApiJwt = secrets.LeafApiJwt;
            if (!string.IsNullOrEmpty(secrets.LeafApiRefreshToken)) settings.LeafApiRefreshToken = secrets.LeafApiRefreshToken;

            if (settings.SavedAccounts != null && secrets.Accounts != null)
            {
                foreach (var acct in settings.SavedAccounts)
                {
                    if (acct == null || string.IsNullOrEmpty(acct.Id)) continue;
                    if (secrets.Accounts.TryGetValue(acct.Id, out var s))
                    {
                        if (!string.IsNullOrEmpty(s.AccessToken)) acct.AccessToken = s.AccessToken;
                        if (!string.IsNullOrEmpty(s.Xuid)) acct.Xuid = s.Xuid;
                        if (!string.IsNullOrEmpty(s.LeafApiJwt)) acct.LeafApiJwt = s.LeafApiJwt;
                        if (!string.IsNullOrEmpty(s.LeafApiRefreshToken)) acct.LeafApiRefreshToken = s.LeafApiRefreshToken;
                    }
                }
            }
        }

        private void WriteSecretsFile(SettingsSecrets secrets)
        {
            try
            {
                var json = JsonSerializer.Serialize(secrets, JsonContext.Default.SettingsSecrets);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                var protectedBytes = CredentialProtection.Protect(bytes);
                var tmpPath = _secretsFilePath + ".tmp";
                var bakPath = _secretsFilePath + ".bak";
                using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    fs.Write(protectedBytes, 0, protectedBytes.Length);
                    fs.Flush();
                    try { fs.Flush(flushToDisk: true); } catch { }
                }
                if (new FileInfo(tmpPath).Length <= 0)
                {
                    try { File.Delete(tmpPath); } catch { }
                    throw new IOException("Refusing to commit zero-byte secrets write");
                }
                try
                {
                    if (File.Exists(_secretsFilePath) && new FileInfo(_secretsFilePath).Length > 0)
                    {
                        try { File.Copy(_secretsFilePath, bakPath, overwrite: true); } catch { }
                    }
                }
                catch { }
                File.Move(tmpPath, _secretsFilePath, overwrite: true);
                if (!OperatingSystem.IsWindows())
                {
                    try
                    {
                        File.SetUnixFileMode(_secretsFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                    }
                    catch (Exception permEx)
                    {
                        try { File.Delete(_secretsFilePath); } catch { }
                        throw new InvalidOperationException(
                            $"Refusing to store credentials at {_secretsFilePath}: cannot set 0600 permissions ({permEx.Message}).",
                            permEx);
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Error("Secrets", $"Failed to write secrets.dat: {ex.Message}");
            }
        }

        private static async Task<string?> TryReadGoodSettingsAsync(string primaryPath, string backupPath)
        {
            const int MIN_VALID_BYTES = 32;
            try
            {
                if (File.Exists(primaryPath) && new FileInfo(primaryPath).Length >= MIN_VALID_BYTES)
                {
                    var raw = await File.ReadAllTextAsync(primaryPath);
                    if (!string.IsNullOrWhiteSpace(raw)) return raw;
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("Settings", $"Primary read error: {ex.Message}; trying .bak");
            }
            try
            {
                if (File.Exists(backupPath) && new FileInfo(backupPath).Length >= MIN_VALID_BYTES)
                {
                    var raw = await File.ReadAllTextAsync(backupPath);
                    if (!string.IsNullOrWhiteSpace(raw)) return raw;
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("Settings", $"Backup read error: {ex.Message}");
            }
            return null;
        }

        private SettingsSecrets? ReadSecretsFile()
        {
            try
            {
                var bakPath = _secretsFilePath + ".bak";
                byte[]? protectedBytes = null;
                if (File.Exists(_secretsFilePath))
                {
                    try
                    {
                        var b = File.ReadAllBytes(_secretsFilePath);
                        if (b.Length > 0) protectedBytes = b;
                    }
                    catch { }
                }
                if (protectedBytes == null && File.Exists(bakPath))
                {
                    try
                    {
                        var b = File.ReadAllBytes(bakPath);
                        if (b.Length > 0)
                        {
                            protectedBytes = b;
                            try { File.Copy(bakPath, _secretsFilePath, overwrite: true); } catch { }
                            LeafLog.Info("Secrets", "Recovered secrets from .bak.");
                        }
                    }
                    catch { }
                }
                if (protectedBytes == null) return null;
                var bytes = CredentialProtection.Unprotect(protectedBytes);
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonSerializer.Deserialize(json, JsonContext.Default.SettingsSecrets);
            }
            catch (Exception ex)
            {
                LeafLog.Error("Secrets", $"Failed to read secrets.dat: {ex.Message}");
                return null;
            }
        }

    }
}
