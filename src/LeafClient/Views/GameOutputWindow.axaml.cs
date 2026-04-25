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

namespace LeafClient.Views
{
    public partial class GameOutputWindow : Window
    {
        public event EventHandler? KillGameRequested;

        private List<LogEntry> _fullLogs = new();
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

            // Batch UI updates to prevent freezing (100ms interval)
            _uiUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _uiUpdateTimer.Tick += ProcessPendingLogs;
            _uiUpdateTimer.Start();
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

        private void OpenLogsFolder(object? sender, RoutedEventArgs e)
        {
            if (System.IO.Directory.Exists(_logFolderPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _logFolderPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch { }
            }
        }

        private async void CopyLog(object? sender, RoutedEventArgs e)
        {
            if (_fullLogs.Count == 0) return;

            var sb = new StringBuilder();
            var logsCopy = _fullLogs.ToList(); // Snapshot for thread safety
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
            var searchBox = sender as TextBox;
            RenderLogs(searchBox?.Text);
        }

        private string SanitizeLog(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            string output = Regex.Replace(input, @"[a-zA-Z0-9]{30,}", "[REDACTED TOKEN]");
            output = Regex.Replace(output, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", "[REDACTED EMAIL]");

            return output;
        }

        private void RenderLogs(string? filter = null)
        {
            var output = this.FindControl<Avalonia.Controls.SelectableTextBlock>("ConsoleOutput");
            if (output == null) return;

            output.Inlines?.Clear();

            bool hasFilter = !string.IsNullOrWhiteSpace(filter);
            var inlinesToAdd = new List<Inline>();

            foreach (var log in _fullLogs)
            {
                if (hasFilter && !log.Message.Contains(filter!, StringComparison.OrdinalIgnoreCase) && !log.Level.Contains(filter!, StringComparison.OrdinalIgnoreCase))
                    continue;

                var runTime = new Run($"{log.Time} [{log.Level}] ") { Foreground = log.LevelColor };
                var runText = new Run($"{log.Message}\n") { Foreground = log.MessageColor };

                inlinesToAdd.Add(runTime);
                inlinesToAdd.Add(runText);
            }

            output.Inlines?.AddRange(inlinesToAdd);

            var scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
            scrollViewer?.ScrollToEnd();
        }

        private const int MaxVisibleLogLines = 500; // prevent UI freeze from too many inlines

        // Batched UI Processor
        private void ProcessPendingLogs(object? sender, EventArgs e)
        {
            if (_pendingLogs.IsEmpty) return;

            var output = this.FindControl<Avalonia.Controls.SelectableTextBlock>("ConsoleOutput");
            var scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
            var searchBox = this.FindControl<TextBox>("SearchBox");

            if (output == null || scrollViewer == null) return;

            bool isAtBottom = (scrollViewer.Offset.Y + scrollViewer.Viewport.Height) >= (scrollViewer.Extent.Height - 20);

            string filter = searchBox?.Text ?? "";
            bool hasFilter = !string.IsNullOrWhiteSpace(filter);

            // Drain at most 100 entries per tick to keep the UI responsive
            int processed = 0;
            while (processed < 100 && _pendingLogs.TryDequeue(out var log))
            {
                _fullLogs.Add(log);
                processed++;
            }

            // Trim the full log list if it exceeds the cap
            if (_fullLogs.Count > MaxVisibleLogLines)
                _fullLogs.RemoveRange(0, _fullLogs.Count - MaxVisibleLogLines);

            // Rebuild visible inlines (only when changes occurred)
            if (processed > 0)
            {
                output.Inlines?.Clear();
                var inlines = new List<Inline>(_fullLogs.Count * 2);
                foreach (var log in _fullLogs)
                {
                    if (hasFilter && !log.Message.Contains(filter, StringComparison.OrdinalIgnoreCase) && !log.Level.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        continue;
                    inlines.Add(new Run($"{log.Time} [{log.Level}] ") { Foreground = log.LevelColor });
                    inlines.Add(new Run($"{log.Message}\n") { Foreground = log.MessageColor });
                }
                output.Inlines?.AddRange(inlines);

                if (isAtBottom)
                    scrollViewer.ScrollToEnd();
            }
        }

        public void ClearLog()
        {
            _fullLogs.Clear();
            while (_pendingLogs.TryDequeue(out _)) { }
            _sessionStartTime = DateTime.Now;
            Dispatcher.UIThread.Post(() =>
            {
                var output = this.FindControl<Avalonia.Controls.SelectableTextBlock>("ConsoleOutput");
                output?.Inlines?.Clear();
            });
        }

        public void AppendLog(string text, string level = "INFO")
        {
            // Process text on background thread to save UI resources
            var sanitizedText = SanitizeLog(text);

            IBrush color = level switch
            {
                "ERROR" => Brushes.Red,
                "WARN" => Brushes.Yellow,
                "INFO" => Brushes.LightBlue,
                _ => Brushes.Gray
            };

            var entry = new LogEntry
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Level = level,
                Message = sanitizedText,
                LevelColor = color,
                MessageColor = Brushes.LightGray
            };

            // Queue for batch processing
            _pendingLogs.Enqueue(entry);
        }
    }
}
