using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using LeafClient.Services;

namespace LeafClient.Views
{
    public partial class MainWindow
    {
        private bool _trialOverlayShown;
        private bool _trialEndedOverlayShown;
        private bool _trialOfferAnimating;
        private bool _trialStatusAnimating;
        private bool _trialEndedAnimating;

        private async Task AnimateTrialOverlayInAsync(string overlayName, string panelName, string dimName, string backdropName)
        {
            var overlay  = this.FindControl<Grid>(overlayName);
            var panel    = this.FindControl<Border>(panelName);
            var dim      = this.FindControl<Border>(dimName);
            var backdrop = this.FindControl<Border>(backdropName);
            if (overlay == null || panel == null) return;

            overlay.IsVisible = true;
            if (dim != null) dim.Opacity = 0;
            if (backdrop != null) backdrop.Opacity = 0;
            panel.Opacity = 0;

            var st = panel.RenderTransform as ScaleTransform ?? new ScaleTransform(0.85, 0.85);
            panel.RenderTransform = st;
            panel.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            st.ScaleX = 0.85; st.ScaleY = 0.85;

            if (!AreAnimationsEnabled())
            {
                if (dim != null) dim.Opacity = 1;
                if (backdrop != null) backdrop.Opacity = 1;
                panel.Opacity = 1;
                st.ScaleX = 1; st.ScaleY = 1;
                return;
            }

            const int steps = 18;
            const int durationMs = 240;
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double ease = 1 - Math.Pow(1 - t, 3);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (dim != null) dim.Opacity = ease;
                    if (backdrop != null) backdrop.Opacity = ease;
                    panel.Opacity = ease;
                    st.ScaleX = 0.85 + 0.15 * ease;
                    st.ScaleY = 0.85 + 0.15 * ease;
                });
                if (i < steps) await Task.Delay(durationMs / steps);
            }
        }

        private async Task AnimateTrialOverlayOutAsync(string overlayName, string panelName, string dimName, string backdropName)
        {
            var overlay  = this.FindControl<Grid>(overlayName);
            var panel    = this.FindControl<Border>(panelName);
            var dim      = this.FindControl<Border>(dimName);
            var backdrop = this.FindControl<Border>(backdropName);
            if (overlay == null || panel == null || !overlay.IsVisible) return;

            var st = panel.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);

            if (!AreAnimationsEnabled())
            {
                overlay.IsVisible = false;
                return;
            }

            const int steps = 14;
            const int durationMs = 180;
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (dim != null) dim.Opacity = 1 - t;
                    if (backdrop != null) backdrop.Opacity = 1 - t;
                    panel.Opacity = 1 - t;
                    st.ScaleX = 1 - 0.1 * t;
                    st.ScaleY = 1 - 0.1 * t;
                });
                if (i < steps) await Task.Delay(durationMs / steps);
            }
            overlay.IsVisible = false;
        }

        private async Task MaybeShowTrialOverlayAsync()
        {
            try
            {
                if (_currentSettings == null) return;
                var jwt = _currentSettings.LeafApiJwt;
                if (string.IsNullOrEmpty(jwt)) return;
                var balance = await LeafApiService.GetUserBalanceAsync(jwt);
                if (balance == null) return;

                _hadLeafPlusTrial = balance.HadLeafPlusTrial;
                _isTrial = balance.IsTrial;

                string? userId = DecodeJwtSubject(jwt);

                TrialService.EligibilityState? eligibility = null;
                if (!balance.IsLeafPlus && !balance.HadLeafPlusTrial && !_currentSettings.TrialPopupDismissed)
                {
                    eligibility = await TrialService.EvaluateAsync(jwt);
                }

                var kind = TrialService.ResolveBannerKind(
                    balance,
                    _currentSettings.TrialPopupDismissed,
                    _currentSettings.TrialEndedSeenForUserId,
                    userId,
                    eligibility);

                if (kind == TrialService.BannerKind.EligibleNotDismissed && !_trialOverlayShown)
                {
                    _trialOverlayShown = true;
                    Dispatcher.UIThread.Post(() => ShowTrialOfferOverlay());
                }
                else if (kind == TrialService.BannerKind.TrialEndedNeedsCta && !_trialEndedOverlayShown)
                {
                    _trialEndedOverlayShown = true;
                    Dispatcher.UIThread.Post(() => ShowTrialEndedOverlay());
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("Trial", $"MaybeShowTrialOverlayAsync error: {ex.Message}");
            }
        }

        private async void ShowTrialOfferOverlay()
        {
            if (_trialOfferAnimating) return;
            var overlay = this.FindControl<Grid>("TrialOfferOverlay");
            if (overlay == null || overlay.IsVisible) return;
            var err = this.FindControl<TextBlock>("TrialOfferErrorText");
            if (err != null) err.IsVisible = false;
            var label = this.FindControl<TextBlock>("TrialOfferStartLabel");
            if (label != null) label.Text = "START FREE TRIAL";

            _trialOfferAnimating = true;
            try { await AnimateTrialOverlayInAsync("TrialOfferOverlay", "TrialOfferPanel", "TrialOfferDim", "TrialOfferBackdrop"); }
            finally { _trialOfferAnimating = false; }
        }

        private async void HideTrialOfferOverlay()
        {
            if (_trialOfferAnimating) return;
            _trialOfferAnimating = true;
            try { await AnimateTrialOverlayOutAsync("TrialOfferOverlay", "TrialOfferPanel", "TrialOfferDim", "TrialOfferBackdrop"); }
            finally { _trialOfferAnimating = false; }
        }

        private async void ShowTrialEndedOverlay()
        {
            if (_trialEndedAnimating) return;
            var overlay = this.FindControl<Grid>("TrialEndedOverlay");
            if (overlay == null || overlay.IsVisible) return;
            _trialEndedAnimating = true;
            try { await AnimateTrialOverlayInAsync("TrialEndedOverlay", "TrialEndedPanel", "TrialEndedDim", "TrialEndedBackdrop"); }
            finally { _trialEndedAnimating = false; }
        }

        private async void HideTrialEndedOverlay()
        {
            if (_trialEndedAnimating) return;
            _trialEndedAnimating = true;
            try { await AnimateTrialOverlayOutAsync("TrialEndedOverlay", "TrialEndedPanel", "TrialEndedDim", "TrialEndedBackdrop"); }
            finally { _trialEndedAnimating = false; }
        }

        private async void ShowTrialStatusOverlay()
        {
            if (_trialStatusAnimating) return;
            var overlay = this.FindControl<Grid>("TrialStatusOverlay");
            if (overlay == null || overlay.IsVisible) return;

            var daysLeftLabel = this.FindControl<TextBlock>("TrialStatusDaysLeft");
            var endsOnLabel   = this.FindControl<TextBlock>("TrialStatusEndsOn");
            var subtitle      = this.FindControl<TextBlock>("TrialStatusSubtitle");

            int daysLeft = TrialService.RemainingDaysFromIso(_leafPlusPeriodEnd);
            string endsOn = "";
            if (!string.IsNullOrEmpty(_leafPlusPeriodEnd) &&
                DateTimeOffset.TryParse(_leafPlusPeriodEnd, out var dt))
            {
                endsOn = $"Ends {dt.ToLocalTime():MMM d, yyyy} at {dt.ToLocalTime():h:mm tt}";
            }

            if (daysLeftLabel != null)
                daysLeftLabel.Text = daysLeft <= 0
                    ? "Ending today"
                    : (daysLeft == 1 ? "1 day left" : $"{daysLeft} days left");
            if (endsOnLabel != null) endsOnLabel.Text = endsOn;
            if (subtitle != null)
                subtitle.Text = daysLeft <= 0
                    ? "Your trial ends very soon."
                    : "Your trial is active.";

            _trialStatusAnimating = true;
            try { await AnimateTrialOverlayInAsync("TrialStatusOverlay", "TrialStatusPanel", "TrialStatusDim", "TrialStatusBackdrop"); }
            finally { _trialStatusAnimating = false; }
        }

        private async void HideTrialStatusOverlay()
        {
            if (_trialStatusAnimating) return;
            _trialStatusAnimating = true;
            try { await AnimateTrialOverlayOutAsync("TrialStatusOverlay", "TrialStatusPanel", "TrialStatusDim", "TrialStatusBackdrop"); }
            finally { _trialStatusAnimating = false; }
        }

        private void OnTrialStatusDismissTapped(object? sender, TappedEventArgs e)
        {
            HideTrialStatusOverlay();
        }

        private void OnTrialStatusUpgradeTapped(object? sender, TappedEventArgs e)
        {
            HideTrialStatusOverlay();
            ShowLeafPlusBenefitsPopup();
        }

        private async void OnTrialOfferStartTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                if (_currentSettings == null) return;
                var jwt = _currentSettings.LeafApiJwt;
                if (string.IsNullOrEmpty(jwt)) return;

                var label = this.FindControl<TextBlock>("TrialOfferStartLabel");
                var errText = this.FindControl<TextBlock>("TrialOfferErrorText");
                if (label != null) label.Text = "STARTING…";
                if (errText != null) errText.IsVisible = false;

                var startButton = this.FindControl<Border>("TrialOfferStartButton");
                if (startButton != null) startButton.IsHitTestVisible = false;

                var outcome = await TrialService.GrantAsync(jwt);

                if (startButton != null) startButton.IsHitTestVisible = true;

                if (outcome == null)
                {
                    if (label != null) label.Text = "START FREE TRIAL";
                    if (errText != null)
                    {
                        errText.Text = "Could not reach LeafClient servers. Please try again.";
                        errText.IsVisible = true;
                    }
                    return;
                }

                if (!outcome.Granted)
                {
                    if (label != null) label.Text = "START FREE TRIAL";
                    if (errText != null)
                    {
                        errText.Text = TrialService.FriendlyIneligibilityMessage(outcome.Reason);
                        errText.IsVisible = true;
                    }
                    return;
                }

                _isLeafPlus = true;
                _isTrial = string.Equals(outcome.LeafPlusTier, "trial", StringComparison.OrdinalIgnoreCase);
                _leafPlusTier = outcome.LeafPlusTier;
                _leafPlusPeriodEnd = outcome.LeafPlusPeriodEnd;
                _hadLeafPlusTrial = true;

                _currentSettings.TrialPopupDismissed = true;
                _currentSettings.LastSeenCosmeticDropMonth = null;
                var activeEntry = _currentSettings.SavedAccounts.FirstOrDefault(a => a.Id == _currentSettings.ActiveAccountId);
                if (activeEntry != null) activeEntry.LastSeenCosmeticDropMonth = null;
                try { await _settingsService.SaveSettingsAsync(_currentSettings); } catch { }
                _cosmeticDropChecked = false;

                HideTrialOfferOverlay();
                ApplyLeafPlusUiState();
                _ = UpdateLeafsBalanceAsync();
            }
            catch (Exception ex)
            {
                LeafLog.Info("Trial", $"start error: {ex.Message}");
            }
        }

        private async void OnTrialOfferDismissTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                if (_currentSettings != null)
                {
                    _currentSettings.TrialPopupDismissed = true;
                    try { await _settingsService.SaveSettingsAsync(_currentSettings); } catch { }
                }
                HideTrialOfferOverlay();
            }
            catch { }
        }

        private void OnTrialOfferBackdropTapped(object? sender, TappedEventArgs e)
        {
            OnTrialOfferDismissTapped(sender, e);
        }

        private async void OnTrialEndedDismissTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                if (_currentSettings != null)
                {
                    string? userId = DecodeJwtSubject(_currentSettings.LeafApiJwt);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        _currentSettings.TrialEndedSeenForUserId = userId;
                        try { await _settingsService.SaveSettingsAsync(_currentSettings); } catch { }
                    }
                }
                HideTrialEndedOverlay();
            }
            catch { }
        }

        private async void OnTrialEndedSubscribeTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                if (_currentSettings != null)
                {
                    string? userId = DecodeJwtSubject(_currentSettings.LeafApiJwt);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        _currentSettings.TrialEndedSeenForUserId = userId;
                        try { await _settingsService.SaveSettingsAsync(_currentSettings); } catch { }
                    }
                }
                HideTrialEndedOverlay();
                ShowLeafPlusBenefitsPopup();
            }
            catch { }
        }

        private static string? DecodeJwtSubject(string? jwt)
        {
            if (string.IsNullOrEmpty(jwt)) return null;
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return null;
                string b64 = parts[1].Replace('-', '+').Replace('_', '/');
                int pad = b64.Length % 4;
                if (pad > 0) b64 += new string('=', 4 - pad);
                var bytes = Convert.FromBase64String(b64);
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("sub", out var sub) &&
                    sub.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return sub.GetString();
                }
            }
            catch { }
            return null;
        }
    }
}
