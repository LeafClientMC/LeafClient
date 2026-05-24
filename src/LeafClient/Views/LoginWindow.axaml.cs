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
using System.Threading;
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

        private bool _offlineIsLoginMode = false;

        private double _bgTargetX, _bgTargetY;
        private double _bgCurrentX, _bgCurrentY;
        private TranslateTransform? _bgTranslate;

        private double _logoPhase;
        private TranslateTransform? _logoFloatTransform;
        private TranslateTransform? _loadingLogoFloatTransform;

        private DispatcherTimer? _animTimer;

        public bool LoginSuccessful { get; private set; } = false;
        public event Action<bool>? LoginCompleted;

        public LoginWindow()
        {
            CmlLib.Core.Auth.Microsoft.JsonConfig.DefaultOptions = LeafClient.Json.Options;
            XboxAuthNet.Game.Msal.MsalSerializationConfig.DefaultSerializerOptions = LeafClient.Json.Options;
            XboxAuthNet.JsonConfig.DefaultOptions = LeafClient.Json.Options;

            InitializeComponent();

            _settingsService = new SettingsService();
            _settings = new LauncherSettings();

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

            var dragSurface = this.FindControl<Border>("DragSurface");
            if (dragSurface != null)
            {
                dragSurface.PointerPressed += (_, e) =>
                {
                    var props = e.GetCurrentPoint(this).Properties;
                    if (!props.IsLeftButtonPressed) return;

                    if (e.ClickCount >= 2)
                    {
                        ToggleMaximizeRestore();
                        return;
                    }

                    if (WindowState == WindowState.Maximized)
                    {
                        try { WindowState = WindowState.Normal; } catch { }
                    }
                    BeginMoveDrag(e);
                };
            }

            this.Opened += (_, _) => AdaptInitialSizeToScreen();

            var bgImage = this.FindControl<Image>("BgImage");
            if (bgImage?.RenderTransform is TransformGroup tg && tg.Children.Count >= 2)
                _bgTranslate = tg.Children[1] as TranslateTransform;

            try
            {
                var ss = new LeafClient.Services.SettingsService();
                _ = Task.Run(async () =>
                {
                    var settings = await ss.LoadSettingsAsync();
                    string slug = settings.BackgroundTheme ?? "aurora";
                    LeafClient.Services.BackgroundThemeService.Instance.SetTheme(slug);
                    var bmp = LeafClient.Services.BackgroundThemeService.Instance.GetBitmap(slug);
                    if (bmp != null && bgImage != null)
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => bgImage.Source = bmp);
                });
            }
            catch (Exception ex) { LeafLog.Info("LoginWindow", $"Bg theme apply failed: {ex.Message}"); }

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

            this.PointerMoved += OnWindowPointerMoved;

            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _animTimer.Tick += OnAnimTick;
            _animTimer.Start();

            this.Loaded += async (_, _) =>
            {
                await Task.Delay(80);
                if (_loginCard != null)
                    _loginCard.Opacity = 1.0;
            };
        }

        private void OnAnimTick(object? sender, EventArgs e)
        {
            const double lerpFactor  = 0.055;
            const double floatAmp    = 5.0;
            const double phaseIncr   = 0.022;

            _bgCurrentX += (_bgTargetX - _bgCurrentX) * lerpFactor;
            _bgCurrentY += (_bgTargetY - _bgCurrentY) * lerpFactor;

            if (_bgTranslate != null)
            {
                _bgTranslate.X = _bgCurrentX;
                _bgTranslate.Y = _bgCurrentY;
            }

            _logoPhase += phaseIncr;
            double floatY = Math.Sin(_logoPhase) * floatAmp;

            if (_logoFloatTransform != null)
                _logoFloatTransform.Y = floatY;

            if (_loadingLogoFloatTransform != null)
                _loadingLogoFloatTransform.Y = floatY;
        }

        private void OnWindowPointerMoved(object? sender, PointerEventArgs e)
        {
            var pos = e.GetPosition(this);
            double nx = (pos.X / Bounds.Width)  - 0.5;
            double ny = (pos.Y / Bounds.Height) - 0.5;

            _bgTargetX = nx * 26.0;
            _bgTargetY = ny * 18.0;
        }

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

        private void MinimizeWindow_Click(object? sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeWindow_Click(object? sender, RoutedEventArgs e)
            => ToggleMaximizeRestore();

        private void ToggleMaximizeRestore()
        {
            try
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            catch (Exception ex)
            {
                LeafLog.Error("LoginWindow", $"ToggleMaximizeRestore failed: {ex.Message}");
            }
        }

        private void AdaptInitialSizeToScreen()
        {
            try
            {
                var screen = Screens?.ScreenFromWindow(this) ?? Screens?.Primary;
                if (screen == null) return;
                double scaling = screen.Scaling > 0 ? screen.Scaling : 1.0;
                double areaW = screen.WorkingArea.Width / scaling;
                double areaH = screen.WorkingArea.Height / scaling;

                double targetW = Math.Min(820, areaW * 0.55);
                targetW = Math.Max(MinWidth, targetW);
                double targetH = Math.Min(Math.Min(areaH * 0.92, targetW * 1.18), 880);
                targetH = Math.Max(MinHeight, targetH);

                if (targetW <= areaW) Width = targetW;
                if (targetH <= areaH) Height = targetH;
            }
            catch (Exception ex)
            {
                LeafLog.Error("LoginWindow", $"AdaptInitialSizeToScreen failed: {ex.Message}");
            }
        }

        private void CloseWindow(object? sender, RoutedEventArgs e)
        {
            LeafLog.Info("LoginWindow", "CloseWindow called");
            _animTimer?.Stop();
            this.Close();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            _animTimer?.Stop();
            LeafLog.Info("LoginWindow", $"OnClosing - LoginSuccessful: {LoginSuccessful}");
            base.OnClosing(e);
        }

        private async void MicrosoftLogin_Click(object? sender, RoutedEventArgs e)
        {
            HideError();
            await SwitchToLoading();
            UpdateLoginStatus("LOGGING IN...", "Using MSAL interactive flow...");

            try
            {
                var app = await MsalClientHelper.BuildApplicationWithCache(LeafClient.Services.AuthConfig.MicrosoftClientId, LeafClient.Services.AuthConfig.BuildMsalCacheSettings());

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

                LeafClient.Services.EmbeddedAuthUiHost.SetHostProvider(() => this);
                bool fellBack = false;

                async Task RunMsalAsync()
                {
                    var loginHandler = JELoginHandlerBuilder.BuildDefault();
                    var authenticator = loginHandler.CreateAuthenticatorWithNewAccount();
                    authenticator.AddMsalOAuth(app, msal => msal.Interactive(LeafClient.Services.AuthConfig.ApplyInteractiveOptions));
                    authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                    authenticator.AddForceJEAuthenticator();
                    var session = await authenticator.ExecuteForLauncherAsync();
                    await HandleSuccessfulLogin(session, capturedRefreshToken);
                }

                try
                {
                    UpdateLoginStatus("LOGGING IN...", "Opening Microsoft login window...");
                    await RunMsalAsync();
                }
                catch (Exception ex) when (LeafClient.Services.EmbeddedAuthUiHost.IsBrokerFailure(ex))
                {
                    LeafClient.Services.LeafLog.Warn("Auth", $"Embedded webview failed, falling back to system browser: {ex.Message}");
                    LeafClient.Services.EmbeddedAuthUiHost.ClearHost();
                    fellBack = true;
                    UpdateLoginStatus("LOGGING IN...", "Embedded login unavailable, opening system browser...");
                    await RunMsalAsync();
                }
                finally
                {
                    if (!fellBack) LeafClient.Services.EmbeddedAuthUiHost.ClearHost();
                }
                return;
            }
            catch (OperationCanceledException)
            {
                ShowError("Microsoft login was cancelled or timed out.");
                await SwitchToForm();
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("Auth", $"MICROSOFT LOGIN ERROR: {ex}");
                ShowError($"Login failed: {ex.Message}");
                await SwitchToForm();
            }
        }

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
                    string? deviceHash = null;
                    try { deviceHash = HwidService.GetDeviceHash(); } catch { }
                    string? referralCode = null;
                    try
                    {
                        var refBox = this.FindControl<TextBox>("OfflineReferralCodeTextBox");
                        var raw = refBox?.Text;
                        if (!string.IsNullOrWhiteSpace(raw)) referralCode = raw.Trim();
                    }
                    catch { }
                    var (result, error) = await LeafApiService.RegisterWithErrorAsync(username, password, deviceHash, referralCode);
                    if (result == null)
                    {
                        ShowError(error ?? "Registration failed. Please try a different username.");
                        await SwitchToForm();
                        return;
                    }
                    apiResult = result;
                }

                LeafLog.Info("LoginWindow", $"Offline {(_offlineIsLoginMode ? "sign-in" : "registration")} succeeded for '{username}'");
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
                LeafClient.Services.LeafLog.Error("Auth", $"Offline action error: {ex.Message}");
                ShowError("Something went wrong. Please try again.");
                await SwitchToForm();
            }
        }

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
                    LeafLog.Info("Login", "Microsoft refresh token captured and saved for silent future refreshes.");
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(session.UUID) && !string.IsNullOrWhiteSpace(session.AccessToken))
                    {
                        UpdateLoginStatus($"WELCOME, {session.Username?.ToUpper()}!", "Linking LeafClient account...");
                        using var apiCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                        var apiTask = LeafApiService.MicrosoftLoginAsync(session.UUID, session.AccessToken);
                        var completed = await Task.WhenAny(apiTask, Task.Delay(Timeout.Infinite, apiCts.Token));
                        if (completed == apiTask)
                        {
                            var apiResult = await apiTask;
                            if (apiResult != null)
                            {
                                _settings.LeafApiJwt          = apiResult.AccessToken;
                                _settings.LeafApiRefreshToken = apiResult.RefreshToken;
                                LeafLog.Info("Login", "LeafClient API linked for Microsoft account.");
                            }
                        }
                        else
                        {
                            LeafLog.Info("Login", "LeafClient API link timed out after 8s; continuing - MainWindow will retry.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LeafLog.Error("Login", $"LeafClient API link failed (non-critical): {ex.Message}");
                }

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
