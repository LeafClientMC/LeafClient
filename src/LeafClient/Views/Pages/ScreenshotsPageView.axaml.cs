using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LeafClient.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LeafClient.Views.Pages
{
    public partial class ScreenshotsPageView : UserControl
    {
        private IMainWindowHost? _host;
        private WrapPanel? _grid;
        private TextBlock? _countLabel;
        private Border? _emptyState;
        private string _sortMode = "newest"; // or "oldest"
        private bool _initialized = false;

        public ScreenshotsPageView()
        {
            InitializeComponent();
        }

        public void SetHost(IMainWindowHost host)
        {
            _host = host;
        }

        public void LoadScreenshotsPage()
        {
            if (!_initialized)
            {
                _grid        = this.FindControl<WrapPanel>("ScreenshotsGrid");
                _countLabel  = this.FindControl<TextBlock>("ScreenshotsCount");
                _emptyState  = this.FindControl<Border>("ScreenshotsEmptyState");
                _initialized = true;
            }
            _ = RefreshAsync();
        }

        private string GetScreenshotsDir()
        {
            var mc = _host?.MinecraftFolder;
            if (string.IsNullOrEmpty(mc)) return "";
            return Path.Combine(mc, "screenshots");
        }

        private async Task RefreshAsync()
        {
            if (_grid == null) return;
            _grid.Children.Clear();

            var dir = GetScreenshotsDir();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                if (_emptyState != null) _emptyState.IsVisible = true;
                if (_countLabel != null) _countLabel.Text = "";
                return;
            }

            // Load files off the UI thread
            FileInfo[] files = await Task.Run(() =>
            {
                try
                {
                    var di = new DirectoryInfo(dir);
                    return di.EnumerateFiles()
                        .Where(f => f.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                                    f.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                    f.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                }
                catch { return Array.Empty<FileInfo>(); }
            });

            if (_sortMode == "newest")
                Array.Sort(files, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
            else
                Array.Sort(files, (a, b) => a.LastWriteTime.CompareTo(b.LastWriteTime));

            if (_emptyState != null) _emptyState.IsVisible = files.Length == 0;
            if (_countLabel != null) _countLabel.Text = files.Length == 1 ? "1 screenshot" : $"{files.Length} screenshots";

            foreach (var f in files)
            {
                var card = BuildThumbnailCard(f);
                _grid.Children.Add(card);
            }
        }

        private Border BuildThumbnailCard(FileInfo file)
        {
            var card = new Border
            {
                Width           = 220,
                Height          = 170,
                Margin          = new Thickness(0, 0, 12, 12),
                CornerRadius    = new CornerRadius(12),
                BorderBrush     = SolidColorBrush.Parse("#1C2A38"),
                BorderThickness = new Thickness(1),
                Background      = SolidColorBrush.Parse("#0B1018"),
                ClipToBounds    = true,
                Cursor          = new Cursor(StandardCursorType.Hand),
            };

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
            };

            // Image — load on background thread
            var image = new Image
            {
                Stretch             = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Stretch,
            };
            Grid.SetRow(image, 0);
            Grid.SetRowSpan(image, 2);

            _ = Task.Run(() =>
            {
                try
                {
                    using var stream = File.OpenRead(file.FullName);
                    var bmp = Bitmap.DecodeToWidth(stream, 440);
                    Dispatcher.UIThread.Post(() => image.Source = bmp);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Screenshots] Failed to decode {file.Name}: {ex.Message}");
                }
            });

            // Gradient overlay for readable filename
            var overlay = new Border
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Height            = 50,
            };
            overlay.Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(0.5, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 0),
                    new GradientStop(Color.FromArgb(180, 0, 0, 0), 1),
                }
            };
            Grid.SetRow(overlay, 1);

            // Filename label
            var label = new TextBlock
            {
                Text                = ShortenFilename(file.Name),
                Foreground          = SolidColorBrush.Parse("#E5E7EB"),
                FontSize            = 11,
                FontWeight          = FontWeight.SemiBold,
                VerticalAlignment   = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin              = new Thickness(10, 0, 0, 8),
                TextTrimming        = TextTrimming.CharacterEllipsis,
                MaxWidth            = 130,
            };
            Grid.SetRow(label, 1);

            // Delete button
            var deleteBtn = new Border
            {
                Width               = 26,
                Height              = 26,
                CornerRadius        = new CornerRadius(13),
                Background          = SolidColorBrush.Parse("#CC000000"),
                BorderBrush         = SolidColorBrush.Parse("#66FFFFFF"),
                BorderThickness     = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment   = VerticalAlignment.Top,
                Margin              = new Thickness(0, 8, 8, 0),
                Cursor              = new Cursor(StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text                = "\U0001F5D1",
                    FontSize            = 11,
                    Foreground          = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                },
            };
            deleteBtn.Tapped += async (_, ev) =>
            {
                ev.Handled = true;
                try { File.Delete(file.FullName); } catch { }
                await RefreshAsync();
            };
            Grid.SetRow(deleteBtn, 0);

            grid.Children.Add(image);
            grid.Children.Add(overlay);
            grid.Children.Add(label);
            grid.Children.Add(deleteBtn);

            card.Child = grid;

            // Tap = open with default viewer
            card.Tapped += (_, _) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = file.FullName,
                        UseShellExecute = true,
                    });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Screenshots] Failed to open {file.Name}: {ex.Message}");
                }
            };

            return card;
        }

        private static string ShortenFilename(string name)
        {
            if (name.Length <= 24) return name;
            return name.Substring(0, 21) + "...";
        }

        private async void OnRefreshScreenshots(object? sender, TappedEventArgs e) => await RefreshAsync();

        private async void OnSortTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Border b && b.Tag is string tag)
            {
                _sortMode = tag;
                HighlightSortTabs(tag);
                await RefreshAsync();
            }
        }

        private void HighlightSortTabs(string active)
        {
            foreach (var (name, tag) in new[] { ("SortBy_Newest", "newest"), ("SortBy_Oldest", "oldest") })
            {
                var tab = this.FindControl<Border>(name);
                if (tab == null) continue;
                bool isActive = tag == active;
                tab.Background = isActive
                    ? SolidColorBrush.Parse("#4CAF50")
                    : SolidColorBrush.Parse("#0F1A24");
                tab.BorderBrush = isActive
                    ? SolidColorBrush.Parse("#4CAF50")
                    : SolidColorBrush.Parse("#1C2A38");
                tab.BorderThickness = new Thickness(1);
                if (tab.Child is TextBlock tb)
                {
                    tb.Foreground = isActive
                        ? Brushes.White
                        : SolidColorBrush.Parse("#9CA3AF");
                }
            }
        }

        private void OnOpenScreenshotsFolder(object? sender, TappedEventArgs e)
        {
            var dir = GetScreenshotsDir();
            if (string.IsNullOrEmpty(dir)) return;
            try
            {
                Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo
                {
                    FileName        = dir,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Screenshots] Failed to open folder: {ex.Message}");
            }
        }
    }
}
