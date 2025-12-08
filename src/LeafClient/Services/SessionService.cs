using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using LeafClient.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using XboxAuthNet.Game.Msal;

namespace LeafClient.Services
{
    public class SessionService
    {
        private readonly SettingsService _settingsService;
        private JELoginHandler? _loginHandler;

        public SessionService()
        {
            _settingsService = new SettingsService();
        }

        private async Task<JELoginHandler> GetLoginHandler()
        {
            if (_loginHandler == null)
            {
                // Build MSAL app cache once; JELoginHandler is reused
                var _ = await MsalClientHelper.BuildApplicationWithCache("499c8d36-be2a-4231-9ebd-ef291b7bb64c");
                _loginHandler = new JELoginHandlerBuilder().Build();
            }
            return _loginHandler;
        }

        public async Task<MSession?> GetCurrentSessionAsync()
        {
            Console.WriteLine("[SessionService.GetCurrentSessionAsync] Method entered.");
            var settings = await _settingsService.LoadSettingsAsync();

            if (!settings.IsLoggedIn)
            {
                Console.WriteLine("[SessionService.GetCurrentSessionAsync] User is not logged in.");
                return null;
            }

            try
            {
                if (settings.AccountType == "microsoft")
                {
                    // Reconstruct from settings
                    var session = new MSession(
                        settings.SessionUsername ?? "Player",
                        settings.SessionAccessToken ?? "",
                        settings.SessionUuid ?? ""
                    );

                    Console.WriteLine($"[SessionService.GetCurrentSessionAsync] Microsoft session reconstructed. Username: '{session.Username}', UUID: '{session.UUID}'");

                    if (session.CheckIsValid())
                    {
                        Console.WriteLine("[SessionService.GetCurrentSessionAsync] Microsoft session is valid.");
                        return session;
                    }

                    Console.WriteLine("[SessionService.GetCurrentSessionAsync] Microsoft session expired. Attempting silent refresh...");

                    // Try silent refresh before logging out
                    try
                    {
                        var app = await MsalClientHelper.BuildApplicationWithCache("499c8d36-be2a-4231-9ebd-ef291b7bb64c");
                        var loginHandler = await GetLoginHandler();

                        var authenticator = loginHandler.CreateAuthenticatorWithNewAccount();
                        authenticator.AddMsalOAuth(app, msal => msal.Silent());
                        authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                        authenticator.AddForceJEAuthenticator();

                        var refreshed = await authenticator.ExecuteForLauncherAsync();
                        if (refreshed.CheckIsValid())
                        {
                            Console.WriteLine("[SessionService.GetCurrentSessionAsync] Silent refresh succeeded.");
                            // Persist refreshed values
                            settings.SessionUsername = refreshed.Username;
                            settings.SessionUuid = refreshed.UUID;
                            settings.SessionAccessToken = refreshed.AccessToken;
                            await _settingsService.SaveSettingsAsync(settings);
                            return refreshed;
                        }

                        Console.WriteLine("[SessionService.GetCurrentSessionAsync] Silent refresh failed; will log out.");
                    }
                    catch (Exception exSilent)
                    {
                        Console.WriteLine($"[SessionService.GetCurrentSessionAsync] Silent refresh error: {exSilent.Message}");
                    }

                    await LogoutAsync();
                    return null;
                }
                else if (settings.AccountType == "offline")
                {
                    if (!string.IsNullOrEmpty(settings.OfflineUsername))
                    {
                        var offline = MSession.CreateOfflineSession(settings.OfflineUsername);
                        Console.WriteLine($"[SessionService.GetCurrentSessionAsync] Offline session created. Username: '{offline.Username}', UUID: '{offline.UUID}'");
                        return offline;
                    }
                    Console.WriteLine("[SessionService.GetCurrentSessionAsync] Offline username is null or empty.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SessionService.GetCurrentSessionAsync] ERROR getting session: {ex.Message}");
            }

            return null;
        }

        public async Task LogoutAsync()
        {
            Console.WriteLine("[SessionService.LogoutAsync] Method entered.");
            var settings = await _settingsService.LoadSettingsAsync();

            try
            {
                if (settings.AccountType == "microsoft" && !string.IsNullOrEmpty(settings.SessionUuid))
                {
                    var loginHandler = await GetLoginHandler();
                    var accounts = loginHandler.AccountManager.GetAccounts();

                    var account = accounts.FirstOrDefault(a =>
                        a.Identifier != null &&
                        a.Identifier.Contains(settings.SessionUuid, StringComparison.OrdinalIgnoreCase));

                    if (account != null)
                    {
                        await loginHandler.Signout(account);
                        Console.WriteLine("[SessionService.LogoutAsync] Successfully removed Microsoft account from cache.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SessionService.LogoutAsync] Failed to remove account from cache: {ex.Message}");
            }

            settings.IsLoggedIn = false;
            settings.AccountType = null;
            settings.SessionUsername = null;
            settings.SessionUuid = null;
            settings.SessionAccessToken = null;
            settings.OfflineUsername = null;

            await _settingsService.SaveSettingsAsync(settings);
            Console.WriteLine("[SessionService.LogoutAsync] Settings cleared.");
        }

        public async Task<bool> IsLoggedInAsync()
        {
            var settings = await _settingsService.LoadSettingsAsync();
            return settings.IsLoggedIn;
        }

        // Hardened: avoid returning "Player" while logged in unless we truly have no data.
        public async Task<string> GetUsernameAsync()
        {
            Console.WriteLine("[SessionService.GetUsernameAsync] Method entered.");
            var settings = await _settingsService.LoadSettingsAsync();

            if (settings.IsLoggedIn)
            {
                if (!string.IsNullOrWhiteSpace(settings.SessionUsername))
                {
                    Console.WriteLine($"[SessionService.GetUsernameAsync] Returning settings username: '{settings.SessionUsername}'");
                    return settings.SessionUsername;
                }

                var session = await GetCurrentSessionAsync();
                if (session?.CheckIsValid() == true && !string.IsNullOrWhiteSpace(session.Username))
                {
                    Console.WriteLine($"[SessionService.GetUsernameAsync] Returning session username: '{session.Username}'");
                    return session.Username;
                }
            }

            if (settings.AccountType == "offline" && !string.IsNullOrWhiteSpace(settings.OfflineUsername))
            {
                Console.WriteLine($"[SessionService.GetUsernameAsync] Returning offline username: '{settings.OfflineUsername}'");
                return settings.OfflineUsername;
            }

            Console.WriteLine("[SessionService.GetUsernameAsync] Returning default fallback: 'Player'");
            return "Player";
        }

        public async Task<string?> GetUuidAsync()
        {
            var settings = await _settingsService.LoadSettingsAsync();

            if (settings.IsLoggedIn && !string.IsNullOrWhiteSpace(settings.SessionUuid))
                return settings.SessionUuid;

            var session = await GetCurrentSessionAsync();
            if (session?.CheckIsValid() == true && !string.IsNullOrWhiteSpace(session.UUID))
                return session.UUID;

            if (settings.AccountType == "offline" && !string.IsNullOrWhiteSpace(settings.OfflineUsername))
                return session?.UUID; // offline session UUID format

            return null;
        }

        // Convenience method to fetch both at once (preferred for rendering)
        public async Task<(string? Username, string? Uuid)> GetIdentityAsync()
        {
            var settings = await _settingsService.LoadSettingsAsync();

            if (settings.IsLoggedIn &&
                (!string.IsNullOrWhiteSpace(settings.SessionUsername) || !string.IsNullOrWhiteSpace(settings.SessionUuid)))
            {
                return (settings.SessionUsername, settings.SessionUuid);
            }

            var session = await GetCurrentSessionAsync();
            if (session?.CheckIsValid() == true)
                return (session.Username, session.UUID);

            if (settings.AccountType == "offline" && !string.IsNullOrWhiteSpace(settings.OfflineUsername))
            {
                var offline = MSession.CreateOfflineSession(settings.OfflineUsername);
                return (offline.Username, offline.UUID);
            }

            return (null, null);
        }

        public async Task<string?> GetAccountTypeAsync()
        {
            var settings = await _settingsService.LoadSettingsAsync();
            return settings.AccountType;
        }
    }
}
