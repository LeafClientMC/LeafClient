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
        private readonly SettingsService _settingsSvc;
        private LauncherSettings _settings;

        private StackPanel? _formPanel;
        private StackPanel? _busyPanel;
        private TextBlock?  _statusHeadline;
        private TextBlock?  _statusDetail;
        private TextBox?    _usernameBox;
        private TextBox?    _passwordBox;
        private TextBox?    _confirmBox;
        private TextBlock?  _actionBtnLabel;
        private TextBlock?  _toggleLabel;
        private Border?     _errorBanner;
        private TextBlock?  _errorText;
        private Border?     _card;
        private Image?      _busyLogo;

        private bool _isSignInMode = false;

        private double _bgTargetX, _bgTargetY;
        private double _bgCurrentX, _bgCurrentY;
        private TranslateTransform? _bgTranslate;

        private double _logoPhase;
        private TranslateTransform? _logoFloat;
        private TranslateTransform? _busyLogoFloat;

        private DispatcherTimer? _animTimer;

        public bool LoginSuccessful { get; private set; } = false;
        public event Action<bool>? LoginCompleted;

        public LoginWindow()
        {
            CmlLib.Core.Auth.Microsoft.JsonConfig.DefaultOptions = LeafClient.Json.Options;
            XboxAuthNet.Game.Msal.MsalSerializationConfig.DefaultSerializerOptions = LeafClient.Json.Options;
            XboxAuthNet.JsonConfig.DefaultOptions = LeafClient.Json.Options;

            InitializeComponent();

            _settingsSvc = new SettingsService();
            _settings = new LauncherSettings();

            _card             = this.FindControl<Border>("LoginCard");
            _formPanel        = this.FindControl<StackPanel>("LoginFormPanel");
            _busyPanel     = this.FindControl<StackPanel>("LoginProcessPanel");
            _statusHeadline        = this.FindControl<TextBlock>("LoginStatusBig");
            _statusDetail      = this.FindControl<TextBlock>("LoginStatusSmall");
            _usernameBox        = this.FindControl<TextBox>("OfflineUsernameTextBox");
            _passwordBox        = this.FindControl<TextBox>("OfflinePasswordTextBox");
            _confirmBox = this.FindControl<TextBox>("OfflineConfirmPasswordTextBox");
            _actionBtnLabel       = this.FindControl<TextBlock>("OfflineActionButtonText");
            _toggleLabel             = this.FindControl<TextBlock>("OfflineToggleText");
            _errorBanner              = this.FindControl<Border>("LoginErrorBanner");
            _errorText          = this.FindControl<TextBlock>("LoginErrorBannerText");
            _busyLogo              = this.FindControl<Image>("LoadingLogoImage");

            var dragSurface = this.FindControl<Border>("DragSurface");
            if (dragSurface != null)
            {
                dragSurface.PointerPressed += (_, e) =>
                {
                    var props = e.GetCurrentPoint(this).Properties;
                    if (!props.IsLeftButtonPressed) return;

                    if (e.ClickCount >= 2)
                    {
                        ToggleMaximize();
                        return;
                    }

                    if (WindowState == WindowState.Maximized)
                    {
                        try { WindowState = WindowState.Normal; } catch { }
                    }
                    BeginMoveDrag(e);
                };
            }

            this.Opened += (_, _) => FitToScreen();

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
                _logoFloat = new TranslateTransform();
                logoImage.RenderTransform = _logoFloat;
                logoImage.RenderTransformOrigin = RelativePoint.Center;
            }
            if (_busyLogo != null)
            {
                _busyLogoFloat = new TranslateTransform();
                _busyLogo.RenderTransform = _busyLogoFloat;
                _busyLogo.RenderTransformOrigin = RelativePoint.Center;
            }

            this.PointerMoved += OnPointerMoved;

            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _animTimer.Tick += OnAnimTick;
            _animTimer.Start();

            this.Loaded += async (_, _) =>
            {
                await Task.Delay(80);
                if (_card != null)
                    _card.Opacity = 1.0;
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

            if (_logoFloat != null)
                _logoFloat.Y = floatY;

            if (_busyLogoFloat != null)
                _busyLogoFloat.Y = floatY;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var pos = e.GetPosition(this);
            double nx = (pos.X / Bounds.Width)  - 0.5;
            double ny = (pos.Y / Bounds.Height) - 0.5;

            _bgTargetX = nx * 26.0;
            _bgTargetY = ny * 18.0;
        }

        private async Task ShowBusy()
        {
            if (_formPanel != null)
            {
                _formPanel.Opacity = 0;
                await Task.Delay(240);
                _formPanel.IsVisible = false;
            }
            if (_busyPanel != null)
            {
                _busyPanel.IsVisible = true;
                await Task.Delay(30);
                _busyPanel.Opacity = 1;
            }
        }

        private async Task ShowForm()
        {
            if (_busyPanel != null)
            {
                _busyPanel.Opacity = 0;
                await Task.Delay(240);
                _busyPanel.IsVisible = false;
            }
            if (_formPanel != null)
            {
                _formPanel.IsVisible = true;
                await Task.Delay(30);
                _formPanel.Opacity = 1;
            }
        }

        private void MinimizeWindow_Click(object? sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeWindow_Click(object? sender, RoutedEventArgs e)
            => ToggleMaximize();

        private void ToggleMaximize()
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

        private void FitToScreen()
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
            await ShowBusy();
            SetStatus("LOGGING IN...", "Using MSAL interactive flow...");

            try
            {
                var app = await MsalClientHelper.BuildApplicationWithCache(LeafClient.Services.AuthConfig.MicrosoftClientId, LeafClient.Services.AuthConfig.BuildMsalCacheSettings());

                string? refreshToken = null;
                app.UserTokenCache.SetAfterAccess(notification =>
                {
                    if (notification.HasStateChanged && refreshToken == null)
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
                                        refreshToken = secret.GetString();
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                });

                LeafClient.Services.EmbeddedAuthUiHost.SetHostProvider(() => this);
                bool usedBrowser = false;

                async Task RunMsalAsync()
                {
                    var loginHandler = JELoginHandlerBuilder.BuildDefault();
                    var authenticator = loginHandler.CreateAuthenticatorWithNewAccount();
                    authenticator.AddMsalOAuth(app, msal => msal.Interactive(LeafClient.Services.AuthConfig.ApplyInteractiveOptions));
                    authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                    authenticator.AddForceJEAuthenticator();
                    var session = await authenticator.ExecuteForLauncherAsync();
                    await FinishLogin(session, refreshToken);
                }

                try
                {
                    SetStatus("LOGGING IN...", "Opening Microsoft login window...");
                    await RunMsalAsync();
                }
                catch (Exception ex) when (LeafClient.Services.EmbeddedAuthUiHost.IsBrokerFailure(ex))
                {
                    LeafClient.Services.LeafLog.Warn("Auth", $"Embedded webview failed, falling back to system browser: {ex.Message}");
                    LeafClient.Services.EmbeddedAuthUiHost.ClearHost();
                    usedBrowser = true;
                    SetStatus("LOGGING IN...", "Embedded login unavailable, opening system browser...");
                    await RunMsalAsync();
                }
                finally
                {
                    if (!usedBrowser) LeafClient.Services.EmbeddedAuthUiHost.ClearHost();
                }
                return;
            }
            catch (OperationCanceledException)
            {
                ShowError("Microsoft login was cancelled or timed out.");
                await ShowForm();
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("Auth", $"MICROSOFT LOGIN ERROR: {ex}");
                ShowError($"Login failed: {ex.Message}");
                await ShowForm();
            }
        }

        private void ToggleOfflineMode_Click(object? sender, RoutedEventArgs e)
        {
            _isSignInMode = !_isSignInMode;
            HideError();

            if (_confirmBox != null)
                _confirmBox.IsVisible = !_isSignInMode;

            if (_actionBtnLabel != null)
                _actionBtnLabel.Text = _isSignInMode ? "Sign In" : "Create Account";

            if (_toggleLabel != null)
                _toggleLabel.Text = _isSignInMode
                    ? "Don't have an account? Register"
                    : "Already have an account? Sign in";
        }

        private async void OfflineAction_Click(object? sender, RoutedEventArgs e)
        {
            HideError();

            string username = _usernameBox?.Text?.Trim() ?? "";
            string password = _passwordBox?.Text ?? "";

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Please enter a username.");
                return;
            }

            if (!IsValidUsername(username))
            {
                ShowError("Username must be 3-16 characters (letters, numbers, underscores only).");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter a password.");
                return;
            }

            if (!_isSignInMode)
            {
                string confirm = _confirmBox?.Text ?? "";
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

            await ShowBusy();

            try
            {
                LeafApiAuthResult? auth;

                if (_isSignInMode)
                {
                    SetStatus("SIGNING IN...", "Verifying your account...");
                    var (result, error) = await LeafApiService.LoginWithErrorAsync(username, password);
                    if (result == null)
                    {
                        ShowError(error ?? "Login failed. Please check your credentials.");
                        await ShowForm();
                        return;
                    }
                    auth = result;
                }
                else
                {
                    SetStatus("CREATING ACCOUNT...", "Setting up your LeafClient account...");
                    string? deviceHash = null;
                    try { deviceHash = HwidService.GetDeviceHash(); } catch { }
                    string? referral = null;
                    try
                    {
                        var refBox = this.FindControl<TextBox>("OfflineReferralCodeTextBox");
                        var raw = refBox?.Text;
                        if (!string.IsNullOrWhiteSpace(raw)) referral = raw.Trim();
                    }
                    catch { }
                    var (result, error) = await LeafApiService.RegisterWithErrorAsync(username, password, deviceHash, referral);
                    if (result == null)
                    {
                        ShowError(error ?? "Registration failed. Please try a different username.");
                        await ShowForm();
                        return;
                    }
                    auth = result;
                }

                LeafLog.Info("LoginWindow", $"Offline {(_isSignInMode ? "sign-in" : "registration")} succeeded for '{username}'");
                SetStatus("SUCCESS!", "Redirecting to launcher...");

                var session = MSession.CreateOfflineSession(username);

                _settings = await _settingsSvc.LoadSettingsAsync();
                _settings.IsLoggedIn         = true;
                _settings.AccountType        = "offline";
                _settings.OfflineUsername    = username;
                _settings.SessionUsername    = session.Username;
                _settings.SessionUuid        = session.UUID;
                _settings.SessionAccessToken = session.AccessToken;
                _settings.LeafApiJwt         = auth.AccessToken;
                _settings.LeafApiRefreshToken = auth.RefreshToken;

                await _settingsSvc.SaveSettingsAsync(_settings);

                LoginSuccessful = true;
                Dispatcher.UIThread.Post(() => LoginCompleted?.Invoke(true));
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("Auth", $"Offline action error: {ex.Message}");
                ShowError("Something went wrong. Please try again.");
                await ShowForm();
            }
        }

        private async Task FinishLogin(MSession? session, string? microsoftRefreshToken = null)
        {
            if (session is not null && session.CheckIsValid())
            {
                SetStatus($"WELCOME, {session.Username?.ToUpper()}!", "Redirecting to launcher...");

                _settings = await _settingsSvc.LoadSettingsAsync();
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
                        SetStatus($"WELCOME, {session.Username?.ToUpper()}!", "Linking LeafClient account...");
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                        var linkTask = LeafApiService.MicrosoftLoginAsync(session.UUID, session.AccessToken);
                        var completed = await Task.WhenAny(linkTask, Task.Delay(Timeout.Infinite, cts.Token));
                        if (completed == linkTask)
                        {
                            var auth = await linkTask;
                            if (auth != null)
                            {
                                _settings.LeafApiJwt          = auth.AccessToken;
                                _settings.LeafApiRefreshToken = auth.RefreshToken;
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

                await _settingsSvc.SaveSettingsAsync(_settings);

                LoginSuccessful = true;
                Dispatcher.UIThread.Post(() => LoginCompleted?.Invoke(true));
            }
            else
            {
                ShowError("Login failed. Invalid session.");
                await ShowForm();
            }
        }

        private bool IsValidUsername(string username)
            => username.Length >= 3 && Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");

        private void SetStatus(string bigText, string smallText)
        {
            if (_statusHeadline != null) _statusHeadline.Text = bigText;
            if (_statusDetail   != null) _statusDetail.Text   = smallText;
        }

        private async void ShowError(string message)
        {
            if (_errorBanner != null && _errorText != null)
            {
                _errorText.Text  = message;
                _errorBanner.IsVisible = true;
                await Task.Delay(4000);
                _errorBanner.IsVisible = false;
            }

            SetStatus("UH-OH!", message);
        }

        private void HideError()
        {
            if (_errorBanner != null)
                _errorBanner.IsVisible = false;
        }
    }
}
