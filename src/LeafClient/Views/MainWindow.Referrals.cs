using System;
using System.Threading;
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
        private LeafApiReferralStats? _referralStatsCache;
        private LeafApiReferralCodeInfo? _referralCodeCache;
        private bool _referralVanityAnimating;
        private CancellationTokenSource? _referralCopyResetCts;

        private async Task RefreshReferralCardAsync()
        {
            try
            {
                var jwt = _currentSettings?.LeafApiJwt;
                if (string.IsNullOrEmpty(jwt))
                {
                    ApplyReferralCardState(null, null);
                    return;
                }

                var stats = await LeafApiService.GetReferralStatsAsync(jwt);
                LeafApiReferralCodeInfo? codeInfo = null;
                if (stats != null)
                {
                    codeInfo = await LeafApiService.GetReferralCodeAsync(jwt);
                }

                _referralStatsCache = stats;
                _referralCodeCache = codeInfo;
                Dispatcher.UIThread.Post(() => ApplyReferralCardState(stats, codeInfo));
            }
            catch (Exception ex)
            {
                LeafLog.Info("Referrals", $"RefreshReferralCardAsync failed: {ex.Message}");
            }
        }

        private void ApplyReferralCardState(LeafApiReferralStats? stats, LeafApiReferralCodeInfo? codeInfo)
        {
            var codeBox = this.FindControl<TextBlock>("ReferralCodeDisplay");
            var counter = this.FindControl<TextBlock>("ReferralStatsCounterLabel");
            var progress = this.FindControl<TextBlock>("ReferralProgressText");
            var editBtn = this.FindControl<Button>("ReferralEditCodeButton");

            if (stats == null)
            {
                if (codeBox != null) codeBox.Text = "-";
                if (counter != null) counter.Text = "0 joined";
                if (editBtn != null) editBtn.IsVisible = false;
                return;
            }

            if (codeBox != null) codeBox.Text = string.IsNullOrEmpty(stats.Code) ? "-" : stats.Code;
            if (counter != null)
            {
                var qualified = stats.QualifiedCount;
                var pending = stats.PendingCount;
                if (pending > 0)
                {
                    counter.Text = $"{qualified} joined • {pending} pending";
                }
                else
                {
                    counter.Text = qualified == 1 ? "1 joined" : $"{qualified} joined";
                }
            }

            if (progress != null)
            {
                if (stats.IsCreator)
                {
                    if (stats.NextMilestone != null)
                    {
                        progress.Text = $"Creator track • {stats.NextMilestone.Count - stats.QualifiedCount} more for next milestone cape • {stats.CreatorLeafPlusCreditDays} Leaf+ days earned.";
                    }
                    else
                    {
                        progress.Text = $"Creator track • All milestone capes earned • {stats.CreatorLeafPlusCreditDays} Leaf+ days earned.";
                    }
                }
                else
                {
                    progress.Text = "Share your code. When someone signs up & plays 30 minutes, you both get rewards.";
                }
            }

            if (editBtn != null) editBtn.IsVisible = codeInfo?.IsCreator == true;
        }

        private async void OnReferralCopyClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var stats = _referralStatsCache;
                if (stats == null || string.IsNullOrEmpty(stats.Code)) return;
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard == null) return;
                await clipboard.SetTextAsync(stats.Code);

                var label = this.FindControl<TextBlock>("ReferralCopyButtonText");
                if (label != null)
                {
                    label.Text = "Copied!";
                    label.Foreground = new SolidColorBrush(Color.Parse("#22C55E"));
                    _referralCopyResetCts?.Cancel();
                    _referralCopyResetCts = new CancellationTokenSource();
                    var token = _referralCopyResetCts.Token;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(1500, token);
                            if (token.IsCancellationRequested) return;
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                label.Text = "Copy";
                                label.Foreground = new SolidColorBrush(Color.Parse("#3B82F6"));
                            });
                        }
                        catch (TaskCanceledException) { }
                    });
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("Referrals", $"Copy failed: {ex.Message}");
            }
        }

        private async void OnReferralEditCodeClick(object? sender, RoutedEventArgs e)
        {
            var info = _referralCodeCache;
            if (info == null || !info.IsCreator) return;

            var input = this.FindControl<TextBox>("ReferralVanityInput");
            var err = this.FindControl<TextBlock>("ReferralVanityErrorText");
            if (input != null) input.Text = info.Code ?? "";
            if (err != null)
            {
                err.IsVisible = false;
                err.Text = "";
                if (!info.CanChangeVanity)
                {
                    var days = (int)Math.Ceiling(info.VanityCooldownMsRemaining / (1000.0 * 60 * 60 * 24));
                    err.Text = days > 0
                        ? $"You can change your code again in {days} day(s)."
                        : "You can change your code now.";
                    err.IsVisible = days > 0;
                }
            }

            await ShowReferralVanityOverlayAsync();
        }

        private async Task ShowReferralVanityOverlayAsync()
        {
            var grid = this.FindControl<Grid>("ReferralVanityOverlay");
            var panel = this.FindControl<Border>("ReferralVanityPanel");
            var dim = this.FindControl<Border>("ReferralVanityDim");
            var backdrop = this.FindControl<Border>("ReferralVanityBackdrop");
            if (grid == null || panel == null) return;

            grid.IsVisible = true;
            if (dim != null) dim.Opacity = 0;
            if (backdrop != null) backdrop.Opacity = 0;
            panel.Opacity = 0;
            panel.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            var st = panel.RenderTransform as ScaleTransform ?? new ScaleTransform(0.85, 0.85);
            panel.RenderTransform = st;
            st.ScaleX = 0.85; st.ScaleY = 0.85;

            const int steps = 16;
            const int durationMs = 200;
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

        private async Task HideReferralVanityOverlayAsync()
        {
            var grid = this.FindControl<Grid>("ReferralVanityOverlay");
            var panel = this.FindControl<Border>("ReferralVanityPanel");
            var dim = this.FindControl<Border>("ReferralVanityDim");
            var backdrop = this.FindControl<Border>("ReferralVanityBackdrop");
            if (grid == null || panel == null) return;
            var st = panel.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);

            const int steps = 12;
            const int durationMs = 160;
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double inv = 1 - t;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (dim != null) dim.Opacity = inv;
                    if (backdrop != null) backdrop.Opacity = inv;
                    panel.Opacity = inv;
                    st.ScaleX = 0.85 + 0.15 * inv;
                    st.ScaleY = 0.85 + 0.15 * inv;
                });
                if (i < steps) await Task.Delay(durationMs / steps);
            }
            grid.IsVisible = false;
        }

        private async void OnReferralVanityCloseTapped(object? sender, TappedEventArgs e)
        {
            if (_referralVanityAnimating) return;
            _referralVanityAnimating = true;
            try { await HideReferralVanityOverlayAsync(); }
            finally { _referralVanityAnimating = false; }
        }

        private async void OnReferralVanityBackdropTapped(object? sender, TappedEventArgs e)
        {
            if (_referralVanityAnimating) return;
            _referralVanityAnimating = true;
            try { await HideReferralVanityOverlayAsync(); }
            finally { _referralVanityAnimating = false; }
        }

        private async void OnReferralVanitySaveTapped(object? sender, TappedEventArgs e)
        {
            var input = this.FindControl<TextBox>("ReferralVanityInput");
            var err = this.FindControl<TextBlock>("ReferralVanityErrorText");
            var saveText = this.FindControl<TextBlock>("ReferralVanitySaveText");
            var raw = (input?.Text ?? "").Trim().ToUpperInvariant();
            if (raw.Length < 3 || raw.Length > 32)
            {
                if (err != null) { err.Text = "Code must be 3-32 letters/numbers."; err.IsVisible = true; }
                return;
            }
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                {
                    if (err != null) { err.Text = "Only uppercase letters and numbers allowed."; err.IsVisible = true; }
                    return;
                }
            }

            var jwt = _currentSettings?.LeafApiJwt;
            if (string.IsNullOrEmpty(jwt))
            {
                if (err != null) { err.Text = "Sign in required."; err.IsVisible = true; }
                return;
            }

            if (saveText != null) saveText.Text = "SAVING...";
            try
            {
                var resp = await LeafApiService.SetReferralVanityCodeAsync(jwt, raw);
                if (resp == null)
                {
                    if (err != null) { err.Text = "Network error. Try again."; err.IsVisible = true; }
                }
                else if (resp.Ok)
                {
                    await HideReferralVanityOverlayAsync();
                    await RefreshReferralCardAsync();
                }
                else
                {
                    var reason = resp.Reason ?? "unknown";
                    string msg = reason switch
                    {
                        "not_a_creator" => "Vanity codes are creator-only.",
                        "cooldown" => "You can change your code once every 30 days.",
                        "invalid_format_or_reserved" => "That code is reserved or in invalid format.",
                        "taken" => "That code is already taken.",
                        _ => "Couldn't save code. Try again.",
                    };
                    if (err != null) { err.Text = msg; err.IsVisible = true; }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("Referrals", $"SetVanityCode failed: {ex.Message}");
                if (err != null) { err.Text = "Network error. Try again."; err.IsVisible = true; }
            }
            finally
            {
                if (saveText != null) saveText.Text = "SAVE";
            }
        }
    }
}
