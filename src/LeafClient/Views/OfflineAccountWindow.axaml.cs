using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LeafClient.Services;

namespace LeafClient.Views
{
    public partial class OfflineAccountWindow : Window
    {
        public sealed class OfflineAccountResult
        {
            public required string Username { get; init; }
            public required LeafApiAuthResult Api { get; init; }
            public required bool WasCreated { get; init; }
        }

        public OfflineAccountResult? Result { get; private set; }

        private bool _createMode;
        private bool _submitting;

        public OfflineAccountWindow()
        {
            InitializeComponent();
            ApplyMode();
            this.Opened += (_, _) =>
            {
                var box = this.FindControl<TextBox>("UsernameBox");
                box?.Focus();
                try
                {
                    var bgImage = this.FindControl<Avalonia.Controls.Image>("BgImage");
                    if (bgImage != null)
                    {
                        var bmp = LeafClient.Services.BackgroundThemeService.Instance.GetCurrentBitmap();
                        if (bmp != null) bgImage.Source = bmp;
                    }
                }
                catch { }
            };
            this.KeyDown += OnKeyDown;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && !_submitting)
            {
                Close();
                return;
            }
            if (e.Key == Key.Enter && !_submitting)
            {
                _ = SubmitAsync();
            }
        }

        private void OnDragZonePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            if (_submitting) return;
            Close();
        }

        private void OnSelectSignInTab(object? sender, RoutedEventArgs e)
        {
            _createMode = false;
            ApplyMode();
            HideError();
        }

        private void OnSelectCreateTab(object? sender, RoutedEventArgs e)
        {
            _createMode = true;
            ApplyMode();
            HideError();
        }

        private void ApplyMode()
        {
            var signInTab = this.FindControl<Button>("SignInTab");
            var createTab = this.FindControl<Button>("CreateTab");
            var signInText = this.FindControl<TextBlock>("SignInTabText");
            var createText = this.FindControl<TextBlock>("CreateTabText");
            var subtext = this.FindControl<TextBlock>("ModeSubtext");
            var confirmSection = this.FindControl<StackPanel>("ConfirmPasswordSection");
            var referralSection = this.FindControl<StackPanel>("ReferralSection");
            var submitText = this.FindControl<TextBlock>("SubmitButtonText");

            var activeBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1B4CAF50"));
            var activeBorder = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#354CAF50"));
            var inactiveBg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10FFFFFF"));
            var inactiveBorder = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#1EFFFFFF"));
            var activeFg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
            var inactiveFg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#90FFFFFF"));

            if (_createMode)
            {
                if (signInTab != null) { signInTab.Background = inactiveBg; signInTab.BorderBrush = inactiveBorder; signInTab.BorderThickness = new Avalonia.Thickness(1); }
                if (createTab != null) { createTab.Background = activeBg; createTab.BorderBrush = activeBorder; createTab.BorderThickness = new Avalonia.Thickness(1); }
                if (signInText != null) signInText.Foreground = inactiveFg;
                if (createText != null) createText.Foreground = activeFg;
                if (subtext != null) subtext.Text = "Pick a password to create a new Leaf account for this cracked username.";
                if (confirmSection != null) confirmSection.IsVisible = true;
                if (referralSection != null) referralSection.IsVisible = true;
                if (submitText != null) submitText.Text = "Create account";
            }
            else
            {
                if (signInTab != null) { signInTab.Background = activeBg; signInTab.BorderBrush = activeBorder; signInTab.BorderThickness = new Avalonia.Thickness(1); }
                if (createTab != null) { createTab.Background = inactiveBg; createTab.BorderBrush = inactiveBorder; createTab.BorderThickness = new Avalonia.Thickness(1); }
                if (signInText != null) signInText.Foreground = activeFg;
                if (createText != null) createText.Foreground = inactiveFg;
                if (subtext != null) subtext.Text = "Sign in with your existing Leaf account credentials.";
                if (confirmSection != null) confirmSection.IsVisible = false;
                if (referralSection != null)
                {
                    referralSection.IsVisible = false;
                    var refBox = this.FindControl<TextBox>("ReferralCodeBox");
                    if (refBox != null) refBox.Text = "";
                }
                if (submitText != null) submitText.Text = "Sign in";
            }
        }

        private void ShowError(string msg)
        {
            var panel = this.FindControl<Border>("ErrorPanel");
            var text = this.FindControl<TextBlock>("ErrorText");
            if (text != null) text.Text = msg;
            if (panel != null) panel.IsVisible = true;
        }

        private void HideError()
        {
            var panel = this.FindControl<Border>("ErrorPanel");
            var text = this.FindControl<TextBlock>("ErrorText");
            if (text != null) text.Text = "";
            if (panel != null) panel.IsVisible = false;
        }

        private void SetSubmitting(bool busy)
        {
            _submitting = busy;
            var btn = this.FindControl<Button>("SubmitButton");
            var spinner = this.FindControl<TextBlock>("SubmitSpinner");
            if (btn != null) btn.IsEnabled = !busy;
            if (spinner != null) spinner.IsVisible = busy;
        }

        private async void OnSubmitClick(object? sender, RoutedEventArgs e)
        {
            await SubmitAsync();
        }

        private async Task SubmitAsync()
        {
            if (_submitting) return;

            var nameBox = this.FindControl<TextBox>("UsernameBox");
            var pwBox = this.FindControl<TextBox>("PasswordBox");
            var confirmBox = this.FindControl<TextBox>("ConfirmPasswordBox");
            var refBox = this.FindControl<TextBox>("ReferralCodeBox");

            string name = nameBox?.Text?.Trim() ?? "";
            string password = pwBox?.Text ?? "";

            if (string.IsNullOrWhiteSpace(name)) { ShowError("Please enter a username."); return; }
            if (name.Length < 3 || name.Length > 16 || !Regex.IsMatch(name, "^[A-Za-z0-9_]+$"))
            {
                ShowError("Username must be 3-16 characters (letters, numbers, underscores only).");
                return;
            }
            if (string.IsNullOrEmpty(password)) { ShowError("Please enter a password."); return; }

            HideError();
            SetSubmitting(true);

            try
            {
                LeafApiAuthResult? apiResult;

                if (!_createMode)
                {
                    LeafLog.Info("Accounts", $"Attempting sign-in for username: {name}");
                    var (loginResult, loginError) = await LeafApiService.LoginWithErrorAsync(name, password);
                    if (loginResult == null)
                    {
                        LeafLog.Error("Accounts", $"Sign-in failed for '{name}': {loginError}");
                        ShowError(loginError ?? "Sign-in failed. Check your username and password.");
                        return;
                    }
                    apiResult = loginResult;
                }
                else
                {
                    string confirm = confirmBox?.Text ?? "";
                    if (password != confirm) { ShowError("Passwords do not match."); return; }
                    var pwErr = LeafApiService.ValidatePasswordStrength(password);
                    if (pwErr != null) { ShowError(pwErr); return; }

                    LeafLog.Info("Accounts", $"Attempting registration for username: {name}");
                    string? deviceHash = null;
                    try { deviceHash = HwidService.GetDeviceHash(); } catch { }
                    string? referral = null;
                    var raw = refBox?.Text;
                    if (!string.IsNullOrWhiteSpace(raw)) referral = raw.Trim();

                    var (registerResult, registerError) = await LeafApiService.RegisterWithErrorAsync(name, password, deviceHash, referral);
                    if (registerResult == null)
                    {
                        LeafLog.Error("Accounts", $"Registration failed for '{name}': {registerError}");
                        ShowError(registerError ?? "Registration failed. Try a different username.");
                        return;
                    }
                    apiResult = registerResult;
                }

                Result = new OfflineAccountResult
                {
                    Username = name,
                    Api = apiResult,
                    WasCreated = _createMode,
                };
                Close();
            }
            catch (Exception ex)
            {
                LeafLog.Error("AddOffline", $"Leaf API call failed: {ex.Message}");
                ShowError("Couldn't reach the LeafClient API. Check your connection.");
            }
            finally
            {
                SetSubmitting(false);
            }
        }
    }
}
