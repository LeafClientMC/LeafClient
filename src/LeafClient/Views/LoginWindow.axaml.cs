using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using LeafClient.Models;
using LeafClient.Services;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using XboxAuthNet.Game.Authenticators;
using XboxAuthNet.Game.Msal;
using XboxAuthNet.Game.Msal.OAuth;

namespace LeafClient.Views
{
    public partial class LoginWindow : Window
    {
        private readonly SettingsService _settingsService;
        private LauncherSettings _settings;

        private Grid? _loginFormPanel;
        private Grid? _loginProcessPanel;
        private TextBlock? _loginStatusBig;
        private TextBlock? _loginStatusSmall;
        private TextBox? _offlineUsernameTextBox;
        private Border? _loginErrorBanner;
        private TextBlock? _loginErrorBannerText;

        public bool LoginSuccessful { get; private set; } = false;

        public event Action<bool>? LoginCompleted;

        public LoginWindow()
        {
            // CRITICAL: wire our AOT-safe Json options into the external libraries
            CmlLib.Core.Auth.Microsoft.JsonConfig.DefaultOptions = LeafClient.Json.Options;
            XboxAuthNet.Game.Msal.MsalSerializationConfig.DefaultSerializerOptions = LeafClient.Json.Options;
            XboxAuthNet.JsonConfig.DefaultOptions = LeafClient.Json.Options;

            InitializeComponent();

            _settingsService = new SettingsService();
            _settings = new LauncherSettings();

            var titleBar = this.FindControl<Border>("TitleBar");
            if (titleBar != null)
            {
                titleBar.PointerPressed += (sender, e) =>
                {
                    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                        BeginMoveDrag(e);
                };
            }

            _loginFormPanel = this.FindControl<Grid>("LoginFormPanel");
            _loginProcessPanel = this.FindControl<Grid>("LoginProcessPanel");
            _loginStatusBig = this.FindControl<TextBlock>("LoginStatusBig");
            _loginStatusSmall = this.FindControl<TextBlock>("LoginStatusSmall");
            _offlineUsernameTextBox = this.FindControl<TextBox>("OfflineUsernameTextBox");
            _loginErrorBanner = this.FindControl<Border>("LoginErrorBanner");
            _loginErrorBannerText = this.FindControl<TextBlock>("LoginErrorBannerText");
        }

        private void MinimizeWindow_Click(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseWindow(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine("[LoginWindow] CloseWindow called - just closing window, not shutting down app");
            this.Close();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            Console.WriteLine($"[LoginWindow] OnClosing - LoginSuccessful: {LoginSuccessful}");

            // Don't prevent the window from closing, but don't shutdown the app either
            base.OnClosing(e);
        }

        // -------------------------
        //  MICROSOFT LOGIN (MSAL Interactive - Universal)
        // -------------------------
        private async void MicrosoftLogin_Click(object? sender, RoutedEventArgs e)
        {
            HideError();
            ShowLoginProcess();
            UpdateLoginStatus("LOGGING IN...", "Using MSAL interactive flow...");

            try
            {
                var app = await MsalClientHelper.BuildApplicationWithCache("499c8d36-be2a-4231-9ebd-ef291b7bb64c");
                var loginHandler = JELoginHandlerBuilder.BuildDefault();
                var authenticator = loginHandler.CreateAuthenticatorWithNewAccount();

                authenticator.AddMsalOAuth(app, msal => msal.Interactive());
                authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                authenticator.AddForceJEAuthenticator();

                UpdateLoginStatus("LOGGING IN...", "Opening system browser for Microsoft login...");
                var session = await authenticator.ExecuteForLauncherAsync();
                await HandleSuccessfulLogin(session);
            }
            catch (OperationCanceledException)
            {
                ShowError("Microsoft login was cancelled or timed out.");
                ShowLoginForm();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"===== MICROSOFT LOGIN ERROR =====\n{ex}");
                ShowError($"Login failed: {ex.Message}");
                ShowLoginForm();
            }
        }

        // Helper method to handle successful session setup and state saving
        private async Task HandleSuccessfulLogin(MSession? session)
        {
            if (session is not null && session.CheckIsValid())
            {
                UpdateLoginStatus($"WELCOME, {session.Username.ToUpper()}!", "Redirecting to launcher...");

                _settings = await _settingsService.LoadSettingsAsync();
                _settings.IsLoggedIn = true;
                _settings.AccountType = "microsoft";
                _settings.SessionUsername = session.Username;
                _settings.SessionUuid = session.UUID;
                _settings.SessionAccessToken = session.AccessToken;
                _settings.SessionXuid = session.Xuid;
                _settings.OfflineUsername = null;

                await _settingsService.SaveSettingsAsync(_settings);

                LoginSuccessful = true;

                // FIRE EVENT ON UI THREAD
                Dispatcher.UIThread.Post(() => LoginCompleted?.Invoke(true));
            }
            else
            {
                ShowError("Login failed. Invalid session.");
                ShowLoginForm();
            }
        }

        // -------------------------
        //  OFFLINE LOGIN
        // -------------------------
        private async void OfflineLogin_Click(object? sender, RoutedEventArgs e)
        {
            HideError();

            if (string.IsNullOrWhiteSpace(_offlineUsernameTextBox?.Text))
            {
                ShowError("Please enter a username.");
                return;
            }

            string username = _offlineUsernameTextBox.Text.Trim();

            if (!IsValidOfflineUsername(username))
            {
                ShowError("Invalid username format.");
                return;
            }

            ShowLoginProcess();
            UpdateLoginStatus("CREATING OFFLINE SESSION...", "Setting up your account...");

            try
            {
                var session = MSession.CreateOfflineSession(username);

                if (!session.CheckIsValid())
                {
                    ShowError("Failed to create offline session.");
                    return;
                }

                UpdateLoginStatus("SUCCESS!", "Redirecting to launcher...");

                _settings = await _settingsService.LoadSettingsAsync();
                _settings.IsLoggedIn = true;
                _settings.AccountType = "offline";
                _settings.OfflineUsername = username;
                _settings.SessionUsername = session.Username;
                _settings.SessionUuid = session.UUID;
                _settings.SessionAccessToken = session.AccessToken;

                await _settingsService.SaveSettingsAsync(_settings);

                LoginSuccessful = true;

                // FIRE EVENT ON UI THREAD
                Dispatcher.UIThread.Post(() => LoginCompleted?.Invoke(true));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Offline login error: {ex.Message}");
                ShowError("Something went wrong.");
                ShowLoginForm();
            }
        }

        private bool IsValidOfflineUsername(string username)
        {
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
        }

        private void ShowLoginForm()
        {
            if (_loginFormPanel != null) _loginFormPanel.IsVisible = true;
            if (_loginProcessPanel != null) _loginProcessPanel.IsVisible = false;
        }

        private void ShowLoginProcess()
        {
            if (_loginFormPanel != null) _loginFormPanel.IsVisible = false;
            if (_loginProcessPanel != null) _loginProcessPanel.IsVisible = true;
        }

        private void UpdateLoginStatus(string bigText, string smallText)
        {
            if (_loginStatusBig != null) _loginStatusBig.Text = bigText;
            if (_loginStatusSmall != null) _loginStatusSmall.Text = smallText;
        }

        private async void ShowError(string message)
        {
            if (_loginErrorBanner != null && _loginErrorBannerText != null)
            {
                _loginErrorBannerText.Text = message;
                _loginErrorBanner.IsVisible = true;
                await Task.Delay(4000);
                _loginErrorBanner.IsVisible = false;
            }

            UpdateLoginStatus("UH-OH!", message);
        }

        private void HideError()
        {
            if (_loginErrorBanner != null)
                _loginErrorBanner.IsVisible = false;
        }
    }
}
