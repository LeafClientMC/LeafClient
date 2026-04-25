using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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
        // ── Services ──────────────────────────────────────────────────────────
        private readonly SettingsService _settingsService;
        private LauncherSettings _settings;

        // ── UI controls ───────────────────────────────────────────────────────
        private StackPanel? _loginFormPanel;
        private StackPanel? _loginProcessPanel;
        private TextBlock?  _loginStatusBig;
        private TextBlock?  _loginStatusSmall;
        private TextBox?    _offlineUsernameTextBox;
        private TextBox?    _offlinePasswordTextBox;
        private TextBox?    _offlineConfirmPasswordTextBox;
        private TextBlock?  _offlineActionButtonText;
        private TextBlock?  _offlineToggleText;
        private Border?     _loginErrorBanner;
        private TextBlock?  _loginErrorBannerText;
        private Border?     _loginCard;
        private Image?      _loadingLogoImage;

        // ── Offline mode state ────────────────────────────────────────────────
        private bool _offlineIsLoginMode = false;

        // ── Parallax state ────────────────────────────────────────────────────
        private double _bgTargetX, _bgTargetY;
        private double _bgCurrentX, _bgCurrentY;
        private TranslateTransform? _bgTranslate;

        // ── Logo float state ──────────────────────────────────────────────────
        private double _logoPhase;
        private TranslateTransform? _logoFloatTransform;
        private TranslateTransform? _loadingLogoFloatTransform;

        // ── Shared animation timer ────────────────────────────────────────────
        private DispatcherTimer? _animTimer;

        // ── Public API ────────────────────────────────────────────────────────
        public bool LoginSuccessful { get; private set; } = false;
        public event Action<bool>? LoginCompleted;

        // ─────────────────────────────────────────────────────────────────────

        public LoginWindow()
        {
            CmlLib.Core.Auth.Microsoft.JsonConfig.DefaultOptions = LeafClient.Json.Options;
            XboxAuthNet.Game.Msal.MsalSerializationConfig.DefaultSerializerOptions = LeafClient.Json.Options;
            XboxAuthNet.JsonConfig.DefaultOptions = LeafClient.Json.Options;

            InitializeComponent();

            _settingsService = new SettingsService();
            _settings = new LauncherSettings();

            // Find all named controls
            _loginCard             = this.FindControl<Border>("LoginCard");
            _loginFormPanel        = this.FindControl<StackPanel>("LoginFormPanel");
            _loginProcessPanel     = this.FindControl<StackPanel>("LoginProcessPanel");
            _loginStatusBig        = this.FindControl<TextBlock>("LoginStatusBig");
            _loginStatusSmall      = this.FindControl<TextBlock>("LoginStatusSmall");
            _offlineUsernameTextBox        = this.FindControl<TextBox>("OfflineUsernameTextBox");
            _offlinePasswordTextBox        = this.FindControl<TextBox>("OfflinePasswordTextBox");
            _offlineConfirmPasswordTextBox = this.FindControl<TextBox>("OfflineConfirmPasswordTextBox");
            _offlineActionButtonText       = this.FindControl<TextBlock>("OfflineActionButtonText");
            _offlineToggleText             = this.FindControl<TextBlock>("OfflineToggleText");
            _loginErrorBanner              = this.FindControl<Border>("LoginErrorBanner");
            _loginErrorBannerText          = this.FindControl<TextBlock>("LoginErrorBannerText");
            _loadingLogoImage              = this.FindControl<Image>("LoadingLogoImage");

            // ── Wire up drag surface ──────────────────────────────────────────
            var dragSurface = this.FindControl<Border>("DragSurface");
            if (dragSurface != null)
            {
                dragSurface.PointerPressed += (_, e) =>
                {
                    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                        BeginMoveDrag(e);
                };
            }

            // ── Set up parallax on bg image ───────────────────────────────────
            var bgImage = this.FindControl<Image>("BgImage");
            if (bgImage?.RenderTransform is TransformGroup tg && tg.Children.Count >= 2)
                _bgTranslate = tg.Children[1] as TranslateTransform;

            // ── Set up logo floating transform (created in code for reliability) ──
            var logoImage = this.FindControl<Image>("LogoImage");
            if (logoImage != null)
            {
                _logoFloatTransform = new TranslateTransform();
                logoImage.RenderTransform = _logoFloatTransform;
                logoImage.RenderTransformOrigin = RelativePoint.Center;
            }
            if (_loadingLogoImage != null)
            {
                _loadingLogoFloatTransform = new TranslateTransform();
                _loadingLogoImage.RenderTransform = _loadingLogoFloatTransform;
                _loadingLogoImage.RenderTransformOrigin = RelativePoint.Center;
            }

            // ── Track mouse for parallax ──────────────────────────────────────
            this.PointerMoved += OnWindowPointerMoved;

            // ── Start unified animation timer (60 fps) ────────────────────────
            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _animTimer.Tick += OnAnimTick;
            _animTimer.Start();

            // ── Fade in card after a brief delay on load ──────────────────────
            this.Loaded += async (_, _) =>
            {
                await Task.Delay(80);
                if (_loginCard != null)
                    _loginCard.Opacity = 1.0;
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // ANIMATION TICK
        // ─────────────────────────────────────────────────────────────────────

        private void OnAnimTick(object? sender, EventArgs e)
        {
            const double lerpFactor  = 0.055;
            const double floatAmp    = 5.0;
            const double phaseIncr   = 0.022;

            // Smoothly lerp background towards target (parallax)
            _bgCurrentX += (_bgTargetX - _bgCurrentX) * lerpFactor;
            _bgCurrentY += (_bgTargetY - _bgCurrentY) * lerpFactor;

            if (_bgTranslate != null)
            {
                _bgTranslate.X = _bgCurrentX;
                _bgTranslate.Y = _bgCurrentY;
            }

            // Float logo (sine wave)
            _logoPhase += phaseIncr;
            double floatY = Math.Sin(_logoPhase) * floatAmp;

            if (_logoFloatTransform != null)
                _logoFloatTransform.Y = floatY;

            if (_loadingLogoFloatTransform != null)
                _loadingLogoFloatTransform.Y = floatY;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PARALLAX MOUSE HANDLER
        // ─────────────────────────────────────────────────────────────────────

        private void OnWindowPointerMoved(object? sender, PointerEventArgs e)
        {
            var pos = e.GetPosition(this);
            double nx = (pos.X / Bounds.Width)  - 0.5;   // –0.5 … +0.5
            double ny = (pos.Y / Bounds.Height) - 0.5;

            _bgTargetX = nx * 26.0;   // ± ~13 px horizontal
            _bgTargetY = ny * 18.0;   // ± ~9  px vertical
        }

        // ─────────────────────────────────────────────────────────────────────
        // PANEL TRANSITIONS
        // ─────────────────────────────────────────────────────────────────────

        private async Task SwitchToLoading()
        {
            if (_loginFormPanel != null)
            {
                _loginFormPanel.Opacity = 0;
                await Task.Delay(240);
                _loginFormPanel.IsVisible = false;
            }
            if (_loginProcessPanel != null)
            {
                _loginProcessPanel.IsVisible = true;
                await Task.Delay(30);
                _loginProcessPanel.Opacity = 1;
            }
        }

        private async Task SwitchToForm()
        {
            if (_loginProcessPanel != null)
            {
                _loginProcessPanel.Opacity = 0;
                await Task.Delay(240);
                _loginProcessPanel.IsVisible = false;
            }
            if (_loginFormPanel != null)
            {
                _loginFormPanel.IsVisible = true;
                await Task.Delay(30);
                _loginFormPanel.Opacity = 1;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // WINDOW CHROME
        // ─────────────────────────────────────────────────────────────────────

        private void MinimizeWindow_Click(object? sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void CloseWindow(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine("[LoginWindow] CloseWindow called");
            _animTimer?.Stop();
            this.Close();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            _animTimer?.Stop();
            Console.WriteLine($"[LoginWindow] OnClosing - LoginSuccessful: {LoginSuccessful}");
            base.OnClosing(e);
        }

        // ─────────────────────────────────────────────────────────────────────
        // MICROSOFT LOGIN
        // ─────────────────────────────────────────────────────────────────────

        private async void MicrosoftLogin_Click(object? sender, RoutedEventArgs e)
        {
            HideError();
            await SwitchToLoading();
            UpdateLoginStatus("LOGGING IN...", "Using MSAL interactive flow...");

            try
            {
                var app = await MsalClientHelper.BuildApplicationWithCache("499c8d36-be2a-4231-9ebd-ef291b7bb64c");

                // Intercept the decrypted MSAL token cache after auth completes so we can
                // extract and persist the Microsoft OAuth refresh token ourselves. This gives
                // us a silent fallback path (direct HTTP refresh) that doesn't depend on
                // MSAL's file cache, so users never need to log in again as long as they
                // launch at least once every ~90 days.
                string? capturedRefreshToken = null;
                app.UserTokenCache.SetAfterAccess(notification =>
                {
                    if (notification.HasStateChanged && capturedRefreshToken == null)
                    {
                        try
                        {
                            var cacheBytes = notification.TokenCache.SerializeMsalV3();
                            using var cacheDoc = System.Text.Json.JsonDocument.Parse(cacheBytes);
                            if (cacheDoc.RootElement.TryGetProperty("RefreshToken", out var rtSection))
                            {
                                foreach (var entry in rtSection.EnumerateObject())
                                {
                                    if (entry.Value.TryGetProperty("secret", out var secret))
                                    {
                                        capturedRefreshToken = secret.GetString();
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                });

                var loginHandler = JELoginHandlerBuilder.BuildDefault();
                var authenticator = loginHandler.CreateAuthenticatorWithNewAccount();

                authenticator.AddMsalOAuth(app, msal => msal.Interactive());
                authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                authenticator.AddForceJEAuthenticator();

                UpdateLoginStatus("LOGGING IN...", "Opening system browser for Microsoft login...");
                var session = await authenticator.ExecuteForLauncherAsync();
                await HandleSuccessfulLogin(session, capturedRefreshToken);
            }
            catch (OperationCanceledException)
            {
                ShowError("Microsoft login was cancelled or timed out.");
                await SwitchToForm();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"===== MICROSOFT LOGIN ERROR =====\n{ex}");
                ShowError($"Login failed: {ex.Message}");
                await SwitchToForm();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // OFFLINE / CRACKED ACCOUNT
        // ─────────────────────────────────────────────────────────────────────

        private void ToggleOfflineMode_Click(object? sender, RoutedEventArgs e)
        {
            _offlineIsLoginMode = !_offlineIsLoginMode;
            HideError();

            if (_offlineConfirmPasswordTextBox != null)
                _offlineConfirmPasswordTextBox.IsVisible = !_offlineIsLoginMode;

            if (_offlineActionButtonText != null)
                _offlineActionButtonText.Text = _offlineIsLoginMode ? "Sign In" : "Create Account";

            if (_offlineToggleText != null)
                _offlineToggleText.Text = _offlineIsLoginMode
                    ? "Don't have an account? Register"
                    : "Already have an account? Sign in";
        }

        private async void OfflineAction_Click(object? sender, RoutedEventArgs e)
        {
            HideError();

            string username = _offlineUsernameTextBox?.Text?.Trim() ?? "";
            string password = _offlinePasswordTextBox?.Text ?? "";

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Please enter a username.");
                return;
            }

            if (!IsValidOfflineUsername(username))
            {
                ShowError("Username must be 3-16 characters (letters, numbers, underscores only).");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter a password.");
                return;
            }

            if (!_offlineIsLoginMode)
            {
                string confirm = _offlineConfirmPasswordTextBox?.Text ?? "";
                if (password != confirm)
                {
                    ShowError("Passwords do not match.");
                    return;
                }
                var pwErr = LeafApiService.ValidatePasswordStrength(password);
                if (pwErr != null)
                {
                    ShowError(pwErr);
                    return;
                }
            }

            await SwitchToLoading();

            try
            {
                LeafApiAuthResult? apiResult;

                if (_offlineIsLoginMode)
                {
                    UpdateLoginStatus("SIGNING IN...", "Verifying your account...");
                    var (result, error) = await LeafApiService.LoginWithErrorAsync(username, password);
                    if (result == null)
                    {
                        ShowError(error ?? "Login failed. Please check your credentials.");
                        await SwitchToForm();
                        return;
                    }
                    apiResult = result;
                }
                else
                {
                    UpdateLoginStatus("CREATING ACCOUNT...", "Setting up your LeafClient account...");
                    var (result, error) = await LeafApiService.RegisterWithErrorAsync(username, password);
                    if (result == null)
                    {
                        ShowError(error ?? "Registration failed. Please try a different username.");
                        await SwitchToForm();
                        return;
                    }
                    apiResult = result;
                }

                UpdateLoginStatus("SUCCESS!", "Redirecting to launcher...");

                var session = MSession.CreateOfflineSession(username);

                _settings = await _settingsService.LoadSettingsAsync();
                _settings.IsLoggedIn         = true;
                _settings.AccountType        = "offline";
                _settings.OfflineUsername    = username;
                _settings.SessionUsername    = session.Username;
                _settings.SessionUuid        = session.UUID;
                _settings.SessionAccessToken = session.AccessToken;
                _settings.LeafApiJwt         = apiResult.AccessToken;
                _settings.LeafApiRefreshToken = apiResult.RefreshToken;

                await _settingsService.SaveSettingsAsync(_settings);

                LoginSuccessful = true;
                Dispatcher.UIThread.Post(() => LoginCompleted?.Invoke(true));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Offline action error: {ex.Message}");
                ShowError("Something went wrong. Please try again.");
                await SwitchToForm();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // SUCCESSFUL LOGIN HANDLER
        // ─────────────────────────────────────────────────────────────────────

        private async Task HandleSuccessfulLogin(MSession? session, string? microsoftRefreshToken = null)
        {
            if (session is not null && session.CheckIsValid())
            {
                UpdateLoginStatus($"WELCOME, {session.Username?.ToUpper()}!", "Redirecting to launcher...");

                _settings = await _settingsService.LoadSettingsAsync();
                _settings.IsLoggedIn         = true;
                _settings.AccountType        = "microsoft";
                _settings.SessionUsername    = session.Username;
                _settings.SessionUuid        = session.UUID;
                _settings.SessionAccessToken = session.AccessToken;
                _settings.SessionXuid        = session.Xuid;
                _settings.OfflineUsername    = null;

                if (!string.IsNullOrWhiteSpace(microsoftRefreshToken))
                {
                    _settings.MicrosoftRefreshToken = microsoftRefreshToken;
                    Console.WriteLine("[Login] Microsoft refresh token captured and saved for silent future refreshes.");
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(session.UUID) && !string.IsNullOrWhiteSpace(session.AccessToken))
                        {
                            var apiResult = await LeafApiService.MicrosoftLoginAsync(session.UUID, session.AccessToken);
                            if (apiResult != null)
                            {
                                _settings.LeafApiJwt          = apiResult.AccessToken;
                                _settings.LeafApiRefreshToken  = apiResult.RefreshToken;
                                await _settingsService.SaveSettingsAsync(_settings);
                                Console.WriteLine("[Login] LeafClient API linked for Microsoft account.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Login] LeafClient API link failed (non-critical): {ex.Message}");
                    }
                });

                await _settingsService.SaveSettingsAsync(_settings);

                LoginSuccessful = true;
                Dispatcher.UIThread.Post(() => LoginCompleted?.Invoke(true));
            }
            else
            {
                ShowError("Login failed. Invalid session.");
                await SwitchToForm();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private bool IsValidOfflineUsername(string username)
            => username.Length >= 3 && Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");

        private void UpdateLoginStatus(string bigText, string smallText)
        {
            if (_loginStatusBig   != null) _loginStatusBig.Text   = bigText;
            if (_loginStatusSmall != null) _loginStatusSmall.Text = smallText;
        }

        private async void ShowError(string message)
        {
            if (_loginErrorBanner != null && _loginErrorBannerText != null)
            {
                _loginErrorBannerText.Text  = message;
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
