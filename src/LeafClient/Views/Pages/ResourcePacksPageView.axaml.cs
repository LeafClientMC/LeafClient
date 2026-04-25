using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LeafClient.Models;
using LeafClient.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Views.Pages
{
    public partial class ResourcePacksPageView : UserControl
    {
        private IMainWindowHost? _host;
        private bool _initialized;
        private string _currentTab = "installed";
        private static readonly HttpClient _http = new HttpClient();
        private bool _browseLoadedOnce;

        // Controls
        private TextBlock? _countLabel;
        private StackPanel? _installedPanel;
        private StackPanel? _installedList;
        private Border? _installedEmpty;
        private StackPanel? _browsePanel;
        private Border? _browseLoading;
        private WrapPanel? _browseGrid;
        private Border? _browseEmpty;
        private Border? _searchBoxContainer;
        private TextBox? _searchBox;

        public ResourcePacksPageView()
        {
            InitializeComponent();
        }

        public void SetHost(IMainWindowHost host) => _host = host;

        public void LoadResourcePacksPage()
        {
            if (!_initialized)
            {
                _countLabel       = this.FindControl<TextBlock>("RpCountLabel");
                _installedPanel   = this.FindControl<StackPanel>("InstalledPanel");
                _installedList    = this.FindControl<StackPanel>("InstalledList");
                _installedEmpty   = this.FindControl<Border>("InstalledEmptyState");
                _browsePanel      = this.FindControl<StackPanel>("BrowsePanel");
                _browseLoading    = this.FindControl<Border>("BrowseLoading");
                _browseGrid       = this.FindControl<WrapPanel>("BrowseGrid");
                _browseEmpty      = this.FindControl<Border>("BrowseEmptyState");
                _searchBoxContainer = this.FindControl<Border>("SearchBoxContainer");
                _searchBox        = this.FindControl<TextBox>("SearchBox");

                if (!_http.DefaultRequestHeaders.UserAgent.Any())
                {
                    _http.DefaultRequestHeaders.Add("User-Agent", "LeafClient/1.1.0 (contact@leafclient.com)");
                }

                _initialized = true;
            }

            _ = RefreshInstalledAsync();
        }

        private string GetResourcePacksDir()
        {
            var mc = _host?.MinecraftFolder;
            if (string.IsNullOrEmpty(mc)) return "";
            return Path.Combine(mc, "resourcepacks");
        }

        // ──────────────────────────────────────────────────────────────
        // INSTALLED TAB
        // ──────────────────────────────────────────────────────────────

        private async Task RefreshInstalledAsync()
        {
            if (_installedList == null) return;
            _installedList.Children.Clear();

            var dir = GetResourcePacksDir();
            if (string.IsNullOrEmpty(dir))
            {
                if (_installedEmpty != null) _installedEmpty.IsVisible = true;
                if (_countLabel != null) _countLabel.Text = "";
                return;
            }

            try { Directory.CreateDirectory(dir); } catch { }

            var entries = await Task.Run(() =>
            {
                try
                {
                    var list = new List<ResourcePackEntry>();
                    var di = new DirectoryInfo(dir);

                    // Zips / jars
                    foreach (var f in di.EnumerateFiles())
                    {
                        if (f.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
                            f.Extension.Equals(".jar", StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(new ResourcePackEntry
                            {
                                Name        = f.Name,
                                FullPath    = f.FullName,
                                IsFolder    = false,
                                SizeBytes   = f.Length,
                                LastWrite   = f.LastWriteTime,
                            });
                        }
                    }

                    // Folder-based packs
                    foreach (var d in di.EnumerateDirectories())
                    {
                        long size = 0;
                        try
                        {
                            size = d.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                        }
                        catch { }
                        list.Add(new ResourcePackEntry
                        {
                            Name      = d.Name,
                            FullPath  = d.FullName,
                            IsFolder  = true,
                            SizeBytes = size,
                            LastWrite = d.LastWriteTime,
                        });
                    }

                    return list.OrderByDescending(e => e.LastWrite).ToList();
                }
                catch { return new List<ResourcePackEntry>(); }
            });

            if (_installedEmpty != null) _installedEmpty.IsVisible = entries.Count == 0;
            if (_countLabel != null)
                _countLabel.Text = entries.Count == 1 ? "1 pack" : $"{entries.Count} packs";

            foreach (var e in entries)
            {
                _installedList.Children.Add(BuildInstalledRow(e));
            }
        }

        private Border BuildInstalledRow(ResourcePackEntry entry)
        {
            var row = new Border
            {
                CornerRadius    = new CornerRadius(12),
                Padding         = new Thickness(14, 12),
                Background      = SolidColorBrush.Parse("#0F1A24"),
                BorderBrush     = SolidColorBrush.Parse("#1C2A38"),
                BorderThickness = new Thickness(1),
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            };

            // Monochrome vector icon in a rounded gradient badge —
            // folder icon for folder packs, zip-file icon for .zip/.jar packs.
            var iconBadge = new Border
            {
                Width             = 38,
                Height            = 38,
                CornerRadius      = new CornerRadius(10),
                Margin            = new Thickness(0, 0, 14, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background        = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.Parse("#1A2535"), 0),
                        new GradientStop(Color.Parse("#0D1620"), 1),
                    },
                },
                BorderBrush     = SolidColorBrush.Parse("#243244"),
                BorderThickness = new Thickness(1),
            };

            var iconPath = new Avalonia.Controls.Shapes.Path
            {
                Fill                = SolidColorBrush.Parse("#9CA3AF"),
                Width               = 20,
                Height              = 20,
                Stretch             = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Data = Geometry.Parse(entry.IsFolder
                    // Folder shape (tab + body)
                    ? "M2,5 L9,5 L11,7 L22,7 L22,19 L2,19 Z"
                    // Zip/package shape: stacked rectangle with a latch
                    : "M3,3 L21,3 L21,21 L3,21 Z M10,3 L10,8 L12,10 L14,8 L14,3 Z"),
            };

            iconBadge.Child = iconPath;
            Grid.SetColumn(iconBadge, 0);
            grid.Children.Add(iconBadge);

            var info = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock
            {
                Text         = entry.Name,
                Foreground   = SolidColorBrush.Parse("#E5E7EB"),
                FontSize     = 13,
                FontWeight   = FontWeight.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            info.Children.Add(new TextBlock
            {
                Text         = $"{FormatSize(entry.SizeBytes)}  \u2022  {entry.LastWrite:yyyy-MM-dd}",
                Foreground   = SolidColorBrush.Parse("#6B7280"),
                FontSize     = 10,
            });
            Grid.SetColumn(info, 1);
            grid.Children.Add(info);

            var actions = new StackPanel
            {
                Orientation       = Orientation.Horizontal,
                Spacing           = 6,
                VerticalAlignment = VerticalAlignment.Center,
            };

            // Open icon: external-link arrow (square with arrow pointing out)
            const string openIconData =
                "M14,3 L21,3 L21,10 M21,3 L12,12 M19,14 L19,19 A2,2 0 0 1 17,21 L5,21 A2,2 0 0 1 3,19 L3,7 A2,2 0 0 1 5,5 L10,5";
            var openBtn = MakeVectorPillButton(openIconData, "#0B1018", "#1C2A38", "#9CA3AF");
            openBtn.Tapped += (_, _) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = entry.FullPath,
                        UseShellExecute = true,
                    });
                }
                catch { }
            };

            // Trash icon: lid + bucket with two grip lines
            const string trashIconData =
                "M3,6 L21,6 M8,6 L8,4 A1,1 0 0 1 9,3 L15,3 A1,1 0 0 1 16,4 L16,6 M5,6 L6,20 A2,2 0 0 0 8,22 L16,22 A2,2 0 0 0 18,20 L19,6 M10,10 L10,17 M14,10 L14,17";
            var deleteBtn = MakeVectorPillButton(trashIconData, "#1A0B0B", "#3B1F1F", "#F87171");
            deleteBtn.Tapped += async (_, _) =>
            {
                try
                {
                    if (entry.IsFolder) Directory.Delete(entry.FullPath, true);
                    else File.Delete(entry.FullPath);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ResourcePacks] Delete failed: {ex.Message}");
                }
                await RefreshInstalledAsync();
            };

            actions.Children.Add(openBtn);
            actions.Children.Add(deleteBtn);
            Grid.SetColumn(actions, 2);
            grid.Children.Add(actions);

            row.Child = grid;
            return row;
        }

        private static Border MakePillButton(string glyph, string bg, string border, string fg)
        {
            return new Border
            {
                Width           = 32,
                Height          = 32,
                CornerRadius    = new CornerRadius(8),
                Background      = SolidColorBrush.Parse(bg),
                BorderBrush     = SolidColorBrush.Parse(border),
                BorderThickness = new Thickness(1),
                Cursor          = new Cursor(StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text                = glyph,
                    FontSize            = 13,
                    Foreground          = SolidColorBrush.Parse(fg),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                },
            };
        }

        /// <summary>
        /// Pill button variant that renders an inline Path vector icon instead of a text glyph,
        /// stroked in the target foreground color. Stroke-based rendering (rather than fill) keeps
        /// the icons looking like outlined glyphs consistent with the rest of the UI.
        /// </summary>
        private static Border MakeVectorPillButton(string pathData, string bg, string border, string fg)
        {
            var fgBrush = SolidColorBrush.Parse(fg);
            return new Border
            {
                Width           = 32,
                Height          = 32,
                CornerRadius    = new CornerRadius(8),
                Background      = SolidColorBrush.Parse(bg),
                BorderBrush     = SolidColorBrush.Parse(border),
                BorderThickness = new Thickness(1),
                Cursor          = new Cursor(StandardCursorType.Hand),
                Child = new Avalonia.Controls.Shapes.Path
                {
                    Data                = Geometry.Parse(pathData),
                    Stroke              = fgBrush,
                    StrokeThickness     = 1.8,
                    StrokeLineCap       = PenLineCap.Round,
                    StrokeJoin           = PenLineJoin.Round,
                    Width               = 16,
                    Height              = 16,
                    Stretch             = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                },
            };
        }

        // ──────────────────────────────────────────────────────────────
        // BROWSE TAB (Modrinth)
        // ──────────────────────────────────────────────────────────────

        private async Task LoadBrowseAsync(string? query = null)
        {
            if (_browseGrid == null) return;

            _browseGrid.Children.Clear();
            if (_browseEmpty != null)   _browseEmpty.IsVisible   = false;
            if (_browseLoading != null) _browseLoading.IsVisible = true;

            try
            {
                string url;
                if (string.IsNullOrWhiteSpace(query))
                {
                    url = "https://api.modrinth.com/v2/search?limit=30&index=downloads"
                        + "&facets=" + Uri.EscapeDataString("[[\"project_type:resourcepack\"]]");
                }
                else
                {
                    url = $"https://api.modrinth.com/v2/search?limit=30&query={Uri.EscapeDataString(query)}"
                        + "&facets=" + Uri.EscapeDataString("[[\"project_type:resourcepack\"]]");
                }

                var json = await _http.GetStringAsync(url);
                var response = JsonSerializer.Deserialize(json, LeafClient.JsonContext.Default.ModrinthSearchResponse);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_browseLoading != null) _browseLoading.IsVisible = false;
                    if (response?.hits == null || response.hits.Count == 0)
                    {
                        if (_browseEmpty != null) _browseEmpty.IsVisible = true;
                        return;
                    }
                    foreach (var proj in response.hits)
                    {
                        _browseGrid.Children.Add(BuildBrowseCard(proj));
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ResourcePacks] Browse failed: {ex.Message}");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_browseLoading != null) _browseLoading.IsVisible = false;
                    if (_browseEmpty != null)   _browseEmpty.IsVisible   = true;
                });
            }
        }

        private Border BuildBrowseCard(ModrinthProject proj)
        {
            var card = new Border
            {
                Width           = 220,
                Margin          = new Thickness(0, 0, 12, 12),
                CornerRadius    = new CornerRadius(14),
                Background      = SolidColorBrush.Parse("#0F1A24"),
                BorderBrush     = SolidColorBrush.Parse("#1C2A38"),
                BorderThickness = new Thickness(1),
                ClipToBounds    = true,
            };

            var grid = new Grid { RowDefinitions = new RowDefinitions("120,Auto,Auto,Auto") };

            // Icon
            var iconBorder = new Border
            {
                Background   = SolidColorBrush.Parse("#0B1018"),
                CornerRadius = new CornerRadius(10),
                Margin       = new Thickness(12, 12, 12, 6),
                ClipToBounds = true,
            };
            var image = new Image
            {
                Stretch             = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Stretch,
            };
            iconBorder.Child = image;
            Grid.SetRow(iconBorder, 0);
            grid.Children.Add(iconBorder);

            if (!string.IsNullOrEmpty(proj.icon_url))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var bytes = await _http.GetByteArrayAsync(proj.icon_url);
                        using var ms = new MemoryStream(bytes);
                        var bmp = new Bitmap(ms);
                        await Dispatcher.UIThread.InvokeAsync(() => image.Source = bmp);
                    }
                    catch { }
                });
            }

            // Title
            grid.Children.Add(WithRow(new TextBlock
            {
                Text         = proj.title ?? "",
                Foreground   = SolidColorBrush.Parse("#E5E7EB"),
                FontWeight   = FontWeight.Bold,
                FontSize     = 13,
                Margin       = new Thickness(14, 4, 14, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
            }, 1));

            // Description
            var desc = proj.description ?? "";
            if (desc.Length > 90) desc = desc.Substring(0, 87) + "...";
            grid.Children.Add(WithRow(new TextBlock
            {
                Text         = desc,
                Foreground   = SolidColorBrush.Parse("#9CA3AF"),
                FontSize     = 11,
                Margin       = new Thickness(14, 4, 14, 6),
                MaxHeight    = 36,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
            }, 2));

            // Footer row: stats + install
            var footer = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Margin            = new Thickness(14, 4, 14, 12),
            };

            var stats = new TextBlock
            {
                Text              = $"\u2B07 {FormatCount(proj.downloads)}   \u2764 {FormatCount(proj.follows)}",
                Foreground        = SolidColorBrush.Parse("#6B7280"),
                FontSize          = 10,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(stats, 0);
            footer.Children.Add(stats);

            var installBtn = new Border
            {
                CornerRadius    = new CornerRadius(8),
                Padding         = new Thickness(10, 5),
                Background      = SolidColorBrush.Parse("#4CAF50"),
                Cursor          = new Cursor(StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text       = "INSTALL",
                    Foreground = Brushes.White,
                    FontSize   = 10,
                    FontWeight = FontWeight.Bold,
                    LetterSpacing = 0.5,
                },
            };
            installBtn.Tapped += async (_, ev) =>
            {
                ev.Handled = true;
                await InstallResourcePackAsync(proj, installBtn);
            };
            Grid.SetColumn(installBtn, 1);
            footer.Children.Add(installBtn);

            Grid.SetRow(footer, 3);
            grid.Children.Add(footer);

            card.Child = grid;
            return card;
        }

        private static Control WithRow(Control c, int row)
        {
            Grid.SetRow(c, row);
            return c;
        }

        private async Task InstallResourcePackAsync(ModrinthProject proj, Border button)
        {
            var label = (button.Child as TextBlock);
            var origText = label?.Text ?? "INSTALL";
            if (label != null) label.Text = "...";

            try
            {
                var versionsUrl = $"https://api.modrinth.com/v2/project/{proj.project_id}/version";
                var versionsJson = await _http.GetStringAsync(versionsUrl);
                var versions = JsonSerializer.Deserialize(versionsJson, LeafClient.JsonContext.Default.ListModrinthVersionDetailed);

                if (versions == null || versions.Count == 0)
                {
                    if (label != null) label.Text = "N/A";
                    return;
                }

                // Prefer a version matching the currently selected Minecraft subversion
                var currentMc = _host?.CurrentSettings?.SelectedSubVersion ?? "";
                var picked = versions.FirstOrDefault(v => v.GameVersions != null && v.GameVersions.Contains(currentMc))
                             ?? versions[0];

                var file = picked.files?.FirstOrDefault(f =>
                    f.filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                if (file == null)
                {
                    if (label != null) label.Text = "FAIL";
                    return;
                }

                var dir = GetResourcePacksDir();
                if (string.IsNullOrEmpty(dir))
                {
                    if (label != null) label.Text = "FAIL";
                    return;
                }
                Directory.CreateDirectory(dir);

                var target = Path.Combine(dir, file.filename);
                var data = await _http.GetByteArrayAsync(file.url);
                await File.WriteAllBytesAsync(target, data);

                if (label != null) label.Text = "INSTALLED";
                button.Background = SolidColorBrush.Parse("#2E7D32");
                button.IsHitTestVisible = false;

                // Refresh installed list in background
                _ = RefreshInstalledAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ResourcePacks] Install failed: {ex.Message}");
                if (label != null) label.Text = origText;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // EVENT HANDLERS
        // ──────────────────────────────────────────────────────────────

        private async void OnTabTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Border b && b.Tag is string tag)
            {
                _currentTab = tag;
                HighlightTabs(tag);
                UpdateTabVisibility();
                if (tag == "installed")
                {
                    await RefreshInstalledAsync();
                }
                else if (tag == "browse" && !_browseLoadedOnce)
                {
                    _browseLoadedOnce = true;
                    await LoadBrowseAsync();
                }
            }
        }

        private void HighlightTabs(string active)
        {
            foreach (var (name, tag) in new[] { ("Tab_Installed", "installed"), ("Tab_Browse", "browse") })
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
                    tb.Foreground = isActive ? Brushes.White : SolidColorBrush.Parse("#9CA3AF");
                }
            }
        }

        private void UpdateTabVisibility()
        {
            if (_installedPanel != null) _installedPanel.IsVisible = _currentTab == "installed";
            if (_browsePanel != null)    _browsePanel.IsVisible    = _currentTab == "browse";
            if (_searchBoxContainer != null) _searchBoxContainer.IsVisible = _currentTab == "browse";
        }

        private async void OnSearchKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _searchBox != null)
            {
                await LoadBrowseAsync(_searchBox.Text);
            }
        }

        private async void OnRefreshTapped(object? sender, TappedEventArgs e)
        {
            if (_currentTab == "installed")
                await RefreshInstalledAsync();
            else
                await LoadBrowseAsync(_searchBox?.Text);
        }

        private void OnOpenResourcePacksFolder(object? sender, TappedEventArgs e)
        {
            var dir = GetResourcePacksDir();
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
                Console.Error.WriteLine($"[ResourcePacks] Open folder failed: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────────
        // HELPERS
        // ──────────────────────────────────────────────────────────────

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{(bytes / 1024.0 / 1024.0):0.0} MB";
            return $"{(bytes / 1024.0 / 1024.0 / 1024.0):0.00} GB";
        }

        private static string FormatCount(long n)
        {
            if (n >= 1_000_000) return (n / 1_000_000.0).ToString("0.0") + "M";
            if (n >= 1_000) return (n / 1_000.0).ToString("0.0") + "K";
            return n.ToString();
        }

        private class ResourcePackEntry
        {
            public string Name      { get; set; } = "";
            public string FullPath  { get; set; } = "";
            public bool   IsFolder  { get; set; }
            public long   SizeBytes { get; set; }
            public DateTime LastWrite { get; set; }
        }
    }
}
