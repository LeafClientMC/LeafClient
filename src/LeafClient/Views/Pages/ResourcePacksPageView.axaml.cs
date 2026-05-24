using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using LeafClient.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LeafClient.Views.Pages
{
    public partial class ResourcePacksPageView : UserControl
    {
        private IMainWindowHost? _host;
        private bool _initialized;

        private StackPanel? _installedList;
        private Border? _installedEmpty;

        public ResourcePacksPageView()
        {
            InitializeComponent();
        }

        public void SetHost(IMainWindowHost host) => _host = host;

        public void LoadResourcePacksPage()
        {
            if (!_initialized)
            {
                _installedList  = this.FindControl<StackPanel>("InstalledList");
                _installedEmpty = this.FindControl<Border>("InstalledEmptyState");
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

        private async Task RefreshInstalledAsync()
        {
            if (_installedList == null) return;
            _installedList.Children.Clear();

            var dir = GetResourcePacksDir();
            if (string.IsNullOrEmpty(dir))
            {
                if (_installedEmpty != null) _installedEmpty.IsVisible = true;
                return;
            }

            try { Directory.CreateDirectory(dir); } catch { }

            var entries = await Task.Run(() =>
            {
                try
                {
                    var list = new List<ResourcePackEntry>();
                    var di = new DirectoryInfo(dir);

                    foreach (var f in di.EnumerateFiles())
                    {
                        if (f.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
                            f.Extension.Equals(".jar", StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(new ResourcePackEntry
                            {
                                Name      = f.Name,
                                FullPath  = f.FullName,
                                IsFolder  = false,
                                SizeBytes = f.Length,
                                LastWrite = f.LastWriteTime,
                            });
                        }
                    }

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
                    ? "M2,5 L9,5 L11,7 L22,7 L22,19 L2,19 Z"
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

            const string openIconData =
                "M14,3 L21,3 L21,10 M21,3 L12,12 M19,14 L19,19 A2,2 0 0 1 17,21 L5,21 A2,2 0 0 1 3,19 L3,7 A2,2 0 0 1 5,5 L10,5";
            var openBtn = MakeVectorPillButton(openIconData, "#0B1018", "#1C2A38", "#9CA3AF");
            openBtn.Tapped += (_, _) =>
            {
                try
                {
                    if (entry.IsFolder) LeafClient.Utils.SystemUtil.OpenFolder(entry.FullPath);
                    else LeafClient.Utils.SystemUtil.OpenFile(entry.FullPath);
                }
                catch { }
            };

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
                    LeafLog.Error("ResourcePacks", $"Delete failed: {ex.Message}");
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
                    StrokeJoin          = PenLineJoin.Round,
                    Width               = 16,
                    Height              = 16,
                    Stretch             = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                },
            };
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{(bytes / 1024.0 / 1024.0):0.0} MB";
            return $"{(bytes / 1024.0 / 1024.0 / 1024.0):0.00} GB";
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
