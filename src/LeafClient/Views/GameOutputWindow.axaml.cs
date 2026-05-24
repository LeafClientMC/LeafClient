using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LeafClient.Services;

namespace LeafClient.Views
{
    public partial class GameOutputWindow : Window
    {
        public event EventHandler? KillGameRequested;

        private List<LogEntry> _fullLogs = new();
        private readonly List<LogEntry> _archivedLogs = new();
        private readonly object _archiveLock = new();
        private ConcurrentQueue<LogEntry> _pendingLogs = new();
        private string _logFolderPath = "";
        private DateTime _sessionStartTime;
        private DispatcherTimer _sessionTimer;
        private DispatcherTimer _uiUpdateTimer;

        private struct LogEntry
        {
            public string Time;
            public string Level;
            public string Message;
            public IBrush LevelColor;
            public IBrush MessageColor;
        }

        public GameOutputWindow()
        {
            InitializeComponent();
            _sessionStartTime = DateTime.Now;

            _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _sessionTimer.Tick += (s, e) => UpdateSessionTime();
            _sessionTimer.Start();

            _uiUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _uiUpdateTimer.Tick += ProcessPendingLogs;
            _uiUpdateTimer.Start();

            WirePartnerBannerDimensionDisplay();
            WirePartnerBannerEScaleTransform();
            WireConsoleBackgroundTheme();
        }

        private void WireConsoleBackgroundTheme()
        {
            try
            {
                var outer = this.FindControl<Image>("ConsoleBgImage");
                var inner = this.FindControl<Image>("ConsoleBgImageInner");
                var bmp = LeafClient.Services.BackgroundThemeService.Instance.GetCurrentBitmap();
                if (bmp != null)
                {
                    if (outer != null) outer.Source = bmp;
                    if (inner != null) inner.Source = bmp;
                }
                LeafClient.Services.BackgroundThemeService.Instance.ThemeChanged += (_, theme) =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var next = LeafClient.Services.BackgroundThemeService.Instance.GetBitmap(theme.Slug);
                        if (next == null) return;
                        if (outer != null) outer.Source = next;
                        if (inner != null) inner.Source = next;
                    });
                };
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("ConsoleBg", $"Wire failed: {ex.Message}");
            }
        }

        private Image? _partnerBannerEImage;
        private ScaleTransform? _partnerBannerEScale;

        private void WirePartnerBannerEScaleTransform()
        {
            try
            {
                _partnerBannerEImage = this.FindControl<Image>("PartnerBannerEImage");
                if (_partnerBannerEImage != null)
                {
                    _partnerBannerEScale = new ScaleTransform(1, 1);
                    _partnerBannerEImage.RenderTransform = _partnerBannerEScale;
                    _partnerBannerEImage.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                }
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("PartnerBanner", $"Wire scale failed: {ex.Message}");
            }
        }

        private const string PartnerBannerSlotEUrl = "https://swiftservers.org/?ref=leaf&loc=banner-big";
        private const string PartnerBannerSlotCUrl = "https://swiftservers.org/?ref=leaf&loc=banner-small";

        private void WirePartnerBannerDimensionDisplay()
        {
            try
            {
                var slotE = this.FindControl<Border>("PartnerBannerSlotE");
                var slotEText = this.FindControl<TextBlock>("PartnerBannerSlotEDimensions");
                if (slotE != null && slotEText != null)
                {
                    slotE.GetObservable(BoundsProperty).Subscribe(b =>
                    {
                        if (b.Width > 0 && b.Height > 0)
                            slotEText.Text = $"{(int)Math.Round(b.Width)} × {(int)Math.Round(b.Height)} px";
                    });
                }
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("PartnerBanner", $"Wire failed: {ex.Message}");
            }
        }

        private void OnPartnerBannerClick(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                var tag = (sender as Border)?.Tag as string;
                var url = tag == "C" ? PartnerBannerSlotCUrl : PartnerBannerSlotEUrl;
                LeafClient.Utils.SystemUtil.OpenUrl(url);
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("PartnerBanner", $"Click failed: {ex.Message}");
            }
        }

        private System.Threading.CancellationTokenSource? _partnerBannerECts;

        private async void OnPartnerBannerEPointerEntered(object? sender, PointerEventArgs e)
            => await AnimateBannerEScale(1.05, 200);

        private async void OnPartnerBannerEPointerExited(object? sender, PointerEventArgs e)
            => await AnimateBannerEScale(1.00, 200);

        private async System.Threading.Tasks.Task AnimateBannerEScale(double target, int durationMs)
        {
            var st = _partnerBannerEScale;
            if (st == null) return;

            _partnerBannerECts?.Cancel();
            _partnerBannerECts?.Dispose();
            var cts = new System.Threading.CancellationTokenSource();
            _partnerBannerECts = cts;
            var ct = cts.Token;

            double start = st.ScaleX;
            double delta = target - start;
            if (Math.Abs(delta) < 0.001)
            {
                st.ScaleX = st.ScaleY = target;
                return;
            }

            const int steps = 16;
            int delayMs = durationMs / steps;
            try
            {
                for (int i = 1; i <= steps; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    double t = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - t, 3);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        st.ScaleX = st.ScaleY = start + delta * eased;
                    });
                    await System.Threading.Tasks.Task.Delay(delayMs, ct);
                }
                await Dispatcher.UIThread.InvokeAsync(() => { st.ScaleX = st.ScaleY = target; });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("PartnerBanner", $"Animate failed: {ex.Message}");
            }
            finally
            {
                if (_partnerBannerECts == cts) _partnerBannerECts = null;
                cts.Dispose();
            }
        }

        public void SetIsLeafPlus(bool isLeafPlus)
        {
            try
            {
                var slotE = this.FindControl<Border>("PartnerBannerSlotE");
                if (slotE != null)
                    slotE.IsVisible = !isLeafPlus;
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("PartnerBanner", $"SetIsLeafPlus failed: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void SetSessionInfo(string version, string username, string logFolderPath)
        {
            _logFolderPath = logFolderPath;
            var verText = this.FindControl<TextBlock>("VersionText");
            var accText = this.FindControl<TextBlock>("AccountText");

            if (verText != null) verText.Text = version;
            if (accText != null) accText.Text = $"Account: {username}";
        }

        private void UpdateSessionTime()
        {
            var timeText = this.FindControl<TextBlock>("SessionTimeText");
            if (timeText != null)
            {
                var elapsed = DateTime.Now - _sessionStartTime;
                string timeStr = elapsed.TotalHours >= 1
                    ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m"
                    : elapsed.TotalMinutes >= 1
                        ? $"{elapsed.Minutes}m {elapsed.Seconds}s"
                        : $"{elapsed.Seconds}s";

                timeText.Text = $"Session: {timeStr}";
            }
        }

        private void OnDragZonePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private void MinimizeWindow(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaximizeWindow(object? sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseWindow(object? sender, RoutedEventArgs e) => this.Hide();
        private void KillGameProcess(object? sender, RoutedEventArgs e) => KillGameRequested?.Invoke(this, EventArgs.Empty);

        private bool _sessionEnded;

        public void MarkSessionEnded(int? exitCode)
        {
            if (_sessionEnded) return;
            _sessionEnded = true;

            try { _sessionTimer?.Stop(); } catch { }

            var killBtn = this.FindControl<Button>("KillGameButton");
            if (killBtn != null)
            {
                killBtn.IsEnabled = false;
                killBtn.Opacity = 0.45;
            }

            var icon = this.FindControl<TextBlock>("KillGameIcon");
            var label = this.FindControl<TextBlock>("KillGameLabel");

            bool crashed = exitCode.HasValue && exitCode.Value != 0;
            string displayText = crashed
                ? (exitCode.HasValue ? $"Game Crashed (exit {exitCode.Value})" : "Game Crashed")
                : "Game Exited";

            if (icon != null)  icon.Text = crashed ? "!" : "•";
            if (label != null) label.Text = displayText;

            var timeText = this.FindControl<TextBlock>("SessionTimeText");
            if (timeText != null)
            {
                var elapsed = DateTime.Now - _sessionStartTime;
                string timeStr = elapsed.TotalHours >= 1
                    ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m"
                    : elapsed.TotalMinutes >= 1
                        ? $"{elapsed.Minutes}m {elapsed.Seconds}s"
                        : $"{elapsed.Seconds}s";
                timeText.Text = $"{timeStr} (ended)";
            }
        }

        private void OpenLogsFolder(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (!System.IO.Directory.Exists(_logFolderPath))
                {
                    System.IO.Directory.CreateDirectory(_logFolderPath);
                }
                LeafClient.Utils.SystemUtil.OpenFolder(_logFolderPath);
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("ERROR", $"Failed to open logs folder: {ex.Message}");
            }
        }

        private async void CopyLog(object? sender, RoutedEventArgs e)
        {
            List<LogEntry> logsCopy;
            lock (_archiveLock) logsCopy = new List<LogEntry>(_archivedLogs);

            if (logsCopy.Count == 0)
            {
                logsCopy = _fullLogs.ToList();
                if (logsCopy.Count == 0) return;
            }

            var sb = new StringBuilder(logsCopy.Count * 80);
            foreach (var log in logsCopy)
            {
                sb.AppendLine($"{log.Time} [{log.Level}] {log.Message}");
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(sb.ToString());
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
        }

        private string SanitizeLog(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            string output = Regex.Replace(input, @"[a-zA-Z0-9]{30,}", "[REDACTED TOKEN]");
            output = Regex.Replace(output, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", "[REDACTED EMAIL]");

            return output;
        }

        private const int MaxVisibleLogLines = 500;

        private void ProcessPendingLogs(object? sender, EventArgs e)
        {
            if (_pendingLogs.IsEmpty) return;

            var output = this.FindControl<SelectableTextBlock>("ConsoleOutput");
            var scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
            var searchBox = this.FindControl<TextBox>("SearchBox");
            if (output == null || scrollViewer == null) return;

            bool isAtBottom = (scrollViewer.Offset.Y + scrollViewer.Viewport.Height) >= (scrollViewer.Extent.Height - 20);

            string filter = searchBox?.Text ?? "";
            bool hasFilter = !string.IsNullOrWhiteSpace(filter);

            int drainBudget = 100;
            int drained = 0;
            var drainedEntries = new List<LogEntry>(drainBudget);
            while (drained < drainBudget && _pendingLogs.TryDequeue(out var log))
            {
                _fullLogs.Add(log);
                drainedEntries.Add(log);
                drained++;
            }

            if (drainedEntries.Count > 0)
            {
                lock (_archiveLock) _archivedLogs.AddRange(drainedEntries);
            }

            int overflow = _fullLogs.Count - MaxVisibleLogLines;
            if (overflow > 0)
                _fullLogs.RemoveRange(0, overflow);

            if (drained == 0) return;

            if (hasFilter || overflow > 0)
            {
                output.Inlines?.Clear();
                var inlines = new List<Inline>(_fullLogs.Count);
                foreach (var log in _fullLogs)
                {
                    if (hasFilter
                        && !log.Message.Contains(filter, StringComparison.OrdinalIgnoreCase)
                        && !log.Level.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        continue;
                    inlines.Add(new Run($"{log.Time} [{log.Level}] {log.Message}\n") { Foreground = log.LevelColor ?? _colorDefault });
                }
                output.Inlines?.AddRange(inlines);
            }
            else
            {
                var appended = new List<Inline>(drainedEntries.Count);
                foreach (var log in drainedEntries)
                    appended.Add(new Run($"{log.Time} [{log.Level}] {log.Message}\n") { Foreground = log.LevelColor ?? _colorDefault });
                output.Inlines?.AddRange(appended);
            }

            if (isAtBottom)
                scrollViewer.ScrollToEnd();
        }

        public void ClearLog()
        {
            _fullLogs.Clear();
            lock (_archiveLock) _archivedLogs.Clear();
            while (_pendingLogs.TryDequeue(out _)) { }
            _sessionStartTime = DateTime.Now;
            Dispatcher.UIThread.Post(() =>
            {
                var output = this.FindControl<SelectableTextBlock>("ConsoleOutput");
                output?.Inlines?.Clear();
            });
        }

        public void BeginNewSession()
        {
            _sessionEnded = false;
            _sessionStartTime = DateTime.Now;

            try
            {
                if (_sessionTimer != null && !_sessionTimer.IsEnabled)
                    _sessionTimer.Start();
            }
            catch { }

            Dispatcher.UIThread.Post(() =>
            {
                var killBtn = this.FindControl<Button>("KillGameButton");
                if (killBtn != null)
                {
                    killBtn.IsEnabled = true;
                    killBtn.Opacity = 1.0;
                }

                var icon = this.FindControl<TextBlock>("KillGameIcon");
                var label = this.FindControl<TextBlock>("KillGameLabel");
                if (icon != null)  icon.Text  = "✕";
                if (label != null) label.Text = "Kill Game";

                var timeText = this.FindControl<TextBlock>("SessionTimeText");
                if (timeText != null) timeText.Text = "Session: 0s";
            });
        }

        public string GetRecentLogText(int maxEntries = 500)
        {
            try
            {
                var snapshot = _fullLogs.ToArray();
                int start = Math.Max(0, snapshot.Length - maxEntries);
                var sb = new System.Text.StringBuilder(8192);
                for (int i = start; i < snapshot.Length; i++)
                {
                    var e = snapshot[i];
                    sb.Append(e.Time).Append(" [").Append(e.Level).Append("] ").AppendLine(e.Message);
                }
                return sb.ToString();
            }
            catch { return string.Empty; }
        }

        public void AppendLog(string text, string level = "INFO")
        {
            AppendLogInternal(text, level, alsoConsole: true);
        }

        public void AppendLogFromConsole(string text, string level = "INFO")
        {
            AppendLogInternal(text, level, alsoConsole: false);
        }

        private static readonly IBrush _colorError = new SolidColorBrush(Color.Parse("#FF6B6B"));
        private static readonly IBrush _colorWarn  = new SolidColorBrush(Color.Parse("#F5C24E"));
        private static readonly IBrush _colorInfo  = new SolidColorBrush(Color.Parse("#A7F3D0"));
        private static readonly IBrush _colorDebug = new SolidColorBrush(Color.Parse("#9CA3AF"));
        private static readonly IBrush _colorOk    = new SolidColorBrush(Color.Parse("#4ADE80"));
        private static readonly IBrush _colorDefault = new SolidColorBrush(Color.Parse("#C8CCCCCC"));

        private static IBrush BrushForLevel(string level) => level?.ToUpperInvariant() switch
        {
            "ERROR" or "FATAL" or "SEVERE" => _colorError,
            "WARN" or "WARNING" => _colorWarn,
            "INFO" => _colorInfo,
            "DEBUG" or "TRACE" => _colorDebug,
            "OK" or "SUCCESS" => _colorOk,
            _ => _colorDefault
        };

        private void AppendLogInternal(string text, string level, bool alsoConsole)
        {
            var sanitizedText = SanitizeLog(text);
            var color = BrushForLevel(level);

            var entry = new LogEntry
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Level = level,
                Message = sanitizedText,
                LevelColor = color,
                MessageColor = color
            };

            _pendingLogs.Enqueue(entry);

            if (alsoConsole)
            {
                try { Console.WriteLine($"[{level}] {sanitizedText}"); } catch { }
            }
        }
    }
}
