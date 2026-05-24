#pragma warning disable CS8625
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

        private Task<JELoginHandler> GetLoginHandler()
        {
            _loginHandler ??= JELoginHandlerBuilder.BuildDefault();
            return Task.FromResult(_loginHandler);
        }

        public async Task<MSession?> GetCurrentSessionAsync()
        {
            LeafLog.Info("SessionService.GetCurrentSessionAsync", "Method entered.");
            var settings = await _settingsService.LoadSettingsAsync();

            if (!settings.IsLoggedIn)
            {
                LeafLog.Info("SessionService.GetCurrentSessionAsync", "User is not logged in.");
                return null;
            }

            try
            {
                if (settings.AccountType == "microsoft")
                {
                    var session = new MSession(
                        settings.SessionUsername ?? "Player",
                        settings.SessionAccessToken ?? "",
                        settings.SessionUuid ?? ""
                    );

                    LeafLog.Info("SessionService.GetCurrentSessionAsync", $"Microsoft session reconstructed. Username: '{session.Username}', UUID: '{session.UUID}'");

                    LeafLog.Info("SessionService.GetCurrentSessionAsync", "Attempting proactive silent refresh (default account)...");

                    try
                    {
                        var app = await MsalClientHelper.BuildApplicationWithCache(LeafClient.Services.AuthConfig.MicrosoftClientId, LeafClient.Services.AuthConfig.BuildMsalCacheSettings());
                        var loginHandler = await GetLoginHandler();

                        var authenticator = loginHandler.CreateAuthenticatorWithDefaultAccount();
                        authenticator.AddMsalOAuth(app, msal => msal.Silent());
                        authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                        authenticator.AddForceJEAuthenticator();

                        var refreshed = await authenticator.ExecuteForLauncherAsync();
                        if (refreshed != null && refreshed.CheckIsValid())
                        {
                            LeafLog.Info("SessionService.GetCurrentSessionAsync", "Silent refresh succeeded (default account).");
                            settings.SessionUsername = refreshed.Username;
                            settings.SessionUuid = refreshed.UUID;
                            settings.SessionAccessToken = refreshed.AccessToken;
                            settings.SessionXuid = refreshed.Xuid;
                            await _settingsService.SaveSettingsAsync(settings);
                            return refreshed;
                        }

                        LeafLog.Info("SessionService.GetCurrentSessionAsync", "Default account refresh returned invalid session.");
                    }
                    catch (Exception exDefault)
                    {
                        LeafLog.Info("SessionService.GetCurrentSessionAsync", $"Default account refresh error: {exDefault.Message}");
                    }

                    if (!string.IsNullOrWhiteSpace(settings.MicrosoftRefreshToken))
                    {
                        LeafLog.Info("SessionService.GetCurrentSessionAsync", "Trying direct HTTP refresh...");
                        var directSession = await DirectRefreshService.TryRefreshAsync(
                            settings.MicrosoftRefreshToken, settings, _settingsService);
                        if (directSession != null)
                        {
                            LeafLog.Info("SessionService.GetCurrentSessionAsync", "Direct refresh succeeded.");
                            return directSession;
                        }
                        LeafLog.Info("SessionService.GetCurrentSessionAsync", "Direct refresh failed.");
                    }

                    if (session.CheckIsValid())
                    {
                        LeafLog.Info("SessionService.GetCurrentSessionAsync", "All silent refresh paths failed; returning stored session as fallback.");
                        return session;
                    }

                    LeafLog.Info("SessionService.GetCurrentSessionAsync", "Session invalid and all refresh paths failed; logging out.");
                    await LogoutAsync();
                    return null;
                }
                else if (settings.AccountType == "offline")
                {
                    if (!string.IsNullOrEmpty(settings.OfflineUsername))
                    {
                        var offline = MSession.CreateOfflineSession(settings.OfflineUsername);
                        LeafLog.Info("SessionService.GetCurrentSessionAsync", $"Offline session created. Username: '{offline.Username}', UUID: '{offline.UUID}'");
                        return offline;
                    }
                    LeafLog.Info("SessionService.GetCurrentSessionAsync", "Offline username is null or empty.");
                }
            }
            catch (Exception ex)
            {
                LeafLog.Error("SessionService.GetCurrentSessionAsync", $"ERROR getting session: {ex.Message}");
            }

            return null;
        }

        public async Task LogoutAsync()
        {
            LeafLog.Info("SessionService.LogoutAsync", "Method entered.");
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
                        LeafLog.Info("SessionService.LogoutAsync", "Successfully removed Microsoft account from cache.");
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Error("SessionService.LogoutAsync", $"Failed to remove account from cache: {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(settings.LeafApiRefreshToken))
            {
                try { await LeafApiService.LogoutAsync(settings.LeafApiRefreshToken); }
                catch (Exception ex) { LeafLog.Error("SessionService.LogoutAsync", $"LeafClient API logout failed (non-critical): {ex.Message}"); }
            }

            settings.IsLoggedIn = false;
            settings.AccountType = null;
            settings.SessionUsername = null;
            settings.SessionUuid = null;
            settings.SessionAccessToken = null;
            settings.OfflineUsername = null;
            settings.LeafApiJwt = null;
            settings.LeafApiRefreshToken = null;

            await _settingsService.SaveSettingsAsync(settings);
            LeafLog.Info("SessionService.LogoutAsync", "Settings cleared.");
        }

        public async Task<bool> IsLoggedInAsync()
        {
            var settings = await _settingsService.LoadSettingsAsync();
            return settings.IsLoggedIn;
        }

        public async Task<string> GetUsernameAsync()
        {
            LeafLog.Info("SessionService.GetUsernameAsync", "Method entered.");
            var settings = await _settingsService.LoadSettingsAsync();

            if (settings.IsLoggedIn)
            {
                if (!string.IsNullOrWhiteSpace(settings.SessionUsername))
                {
                    LeafLog.Info("SessionService.GetUsernameAsync", $"Returning settings username: '{settings.SessionUsername}'");
                    return settings.SessionUsername;
                }

                var session = await GetCurrentSessionAsync();
                if (session?.CheckIsValid() == true && !string.IsNullOrWhiteSpace(session.Username))
                {
                    LeafLog.Info("SessionService.GetUsernameAsync", $"Returning session username: '{session.Username}'");
                    return session.Username;
                }
            }

            if (settings.AccountType == "offline" && !string.IsNullOrWhiteSpace(settings.OfflineUsername))
            {
                LeafLog.Info("SessionService.GetUsernameAsync", $"Returning offline username: '{settings.OfflineUsername}'");
                return settings.OfflineUsername;
            }

            LeafLog.Info("SessionService.GetUsernameAsync", "Returning default fallback: 'Player'");
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
                return session?.UUID;

            return null;
        }

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
