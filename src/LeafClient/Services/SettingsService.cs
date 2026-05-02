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
                await File.WriteAllTextAsync(_settingsFilePath, jsonString);
                Console.WriteLine("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<LauncherSettings> LoadSettingsAsync()
        {
            if (!File.Exists(_settingsFilePath))
            {
                Console.WriteLine("Settings file not found. Loading default settings.");
                return new LauncherSettings();
            }

            await _fileLock.WaitAsync();
            try
            {
                var jsonString = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize(jsonString, JsonContext.Default.LauncherSettings);

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
                    Console.WriteLine("[Secrets] Plaintext secrets detected — migrating to encrypted secrets.dat.");
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
                        Console.WriteLine($"[Secrets] Migration write failed: {ex.Message}");
                    }
                }

                Console.WriteLine("Settings loaded successfully.");
                return settings;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing settings file: {ex.Message}. Loading default settings.");
                return new LauncherSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}. Loading default settings.");
                return new LauncherSettings();
            }
            finally
            {
                _fileLock.Release();
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
                File.WriteAllBytes(_secretsFilePath, protectedBytes);
                if (!OperatingSystem.IsWindows())
                {
                    try { File.SetUnixFileMode(_secretsFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Secrets] Failed to write secrets.dat: {ex.Message}");
            }
        }

        private SettingsSecrets? ReadSecretsFile()
        {
            try
            {
                if (!File.Exists(_secretsFilePath)) return null;
                var protectedBytes = File.ReadAllBytes(_secretsFilePath);
                if (protectedBytes.Length == 0) return null;
                var bytes = CredentialProtection.Unprotect(protectedBytes);
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonSerializer.Deserialize(json, JsonContext.Default.SettingsSecrets);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Secrets] Failed to read secrets.dat: {ex.Message}");
                return null;
            }
        }

    }
}
