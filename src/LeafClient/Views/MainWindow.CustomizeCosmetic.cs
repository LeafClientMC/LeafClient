using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LeafClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeafClient.Views
{
    public partial class MainWindow
    {
        private static readonly string[] CustomizeColorOrder = new[]
        {
            "red","orange","yellow","lime","green","light-blue",
            "blue","magenta","purple","pink","brown","neutral"
        };

        private static readonly Dictionary<string, string> CustomizeColorHex = new()
        {
            { "red", "#D23A3A" }, { "orange", "#E08644" }, { "yellow", "#E6C84B" },
            { "lime", "#9FD13A" }, { "green", "#3AAE54" }, { "light-blue", "#5CC4E3" },
            { "blue", "#3A7AD2" }, { "magenta", "#C84CAE" }, { "purple", "#9B4CCF" },
            { "pink", "#E88AAE" }, { "brown", "#8A5A3A" },
        };

        private static readonly string[] CustomizeShadeOrder = new[] { "dark", "normal", "pastel" };
        private static readonly string[] CustomizeNeutralOrder = new[] { "black", "gray", "white" };

        private LeafApiCatalogCosmetic? _customizingCosmetic;
        private string _customizingColor = "purple";
        private string _customizingShade = "pastel";
        private double _customizingScale = 1.0;
        private double _customizingOffsetX = 0.0;
        private double _customizingOffsetY = 0.0;
        private double _customizingOffsetZ = 0.0;
        private bool _customizeAnimating;
        private bool _customizeSlidersWired;

        public void ShowCustomizeCosmeticPopup(string cosmeticId)
        {
            _ = ShowCustomizeCosmeticPopupAsync(cosmeticId);
        }

        private async Task ShowCustomizeCosmeticPopupAsync(string cosmeticId)
        {
            try
            {
                var catalog = await LeafApiService.GetCosmeticsCatalogAsync();
                if (catalog == null) return;
                var item = catalog.FirstOrDefault(c => string.Equals(c.Id, cosmeticId, StringComparison.OrdinalIgnoreCase));
                if (item == null) return;
                if (!item.SupportsVariants && !item.SupportsScale && !item.SupportsOffset) return;
                _customizingCosmetic = item;

                var equipped = _currentSettings?.Equipped;
                string? curVariant = ReadEquippedVariantFor(item.Category);
                double? curScale = ReadEquippedScaleFor(item.Category);
                double? curOffX = ReadEquippedOffsetFor(item.Category, 'x');
                double? curOffY = ReadEquippedOffsetFor(item.Category, 'y');
                double? curOffZ = ReadEquippedOffsetFor(item.Category, 'z');

                var initialVariantSlug = curVariant ?? item.DefaultVariant
                    ?? (item.Variants != null && item.Variants.Count > 0 ? item.Variants[0].Slug : null);
                var initialVariant = item.Variants?.FirstOrDefault(v => v.Slug == initialVariantSlug);
                if (initialVariant != null)
                {
                    _customizingColor = initialVariant.Color ?? "purple";
                    _customizingShade = initialVariant.Shade ?? "normal";
                }
                else
                {
                    _customizingColor = "purple"; _customizingShade = "pastel";
                }

                _customizingScale = curScale ?? 1.0;
                _customizingOffsetX = curOffX ?? 0.0;
                _customizingOffsetY = curOffY ?? 0.0;
                _customizingOffsetZ = curOffZ ?? 0.0;

                BuildCustomizeUI(item);
                await OpenCustomizeOverlayAsync();
            }
            catch (Exception ex)
            {
                LeafLog.Info("Customize", $"open failed: {ex.Message}");
            }
        }

        private void BuildCustomizeUI(LeafApiCatalogCosmetic item)
        {
            var title = this.FindControl<TextBlock>("CustomizeTitle");
            if (title != null) title.Text = item.Name;

            var colorSection = this.FindControl<StackPanel>("CustomizeColorSection");
            var colorPanel = this.FindControl<WrapPanel>("CustomizeColorSwatchesPanel");
            var shadeRow = this.FindControl<StackPanel>("CustomizeShadeRow");
            if (colorSection != null && colorPanel != null && shadeRow != null)
            {
                if (item.SupportsVariants && item.Variants != null && item.Variants.Count > 0)
                {
                    colorSection.IsVisible = true;
                    BuildColorSwatches(colorPanel);
                    BuildShadeRow(shadeRow);
                }
                else
                {
                    colorSection.IsVisible = false;
                }
            }

            var adjustSection = this.FindControl<StackPanel>("CustomizeAdjustSection");
            var scaleRow = this.FindControl<Grid>("CustomizeScaleRow");
            var xRow = this.FindControl<Grid>("CustomizeOffsetXRow");
            var yRow = this.FindControl<Grid>("CustomizeOffsetYRow");
            var zRow = this.FindControl<Grid>("CustomizeOffsetZRow");
            bool anyAdjust = item.SupportsScale || item.SupportsOffset;
            if (adjustSection != null) adjustSection.IsVisible = anyAdjust;

            var scaleSlider = this.FindControl<Slider>("CustomizeScaleSlider");
            var xSlider = this.FindControl<Slider>("CustomizeOffsetXSlider");
            var ySlider = this.FindControl<Slider>("CustomizeOffsetYSlider");
            var zSlider = this.FindControl<Slider>("CustomizeOffsetZSlider");

            if (!_customizeSlidersWired)
            {
                if (scaleSlider != null)
                    scaleSlider.PropertyChanged += (_, e) => { if (e.Property == RangeBase.ValueProperty) OnCustomizeScaleChanged((double)e.NewValue!); };
                if (xSlider != null)
                    xSlider.PropertyChanged += (_, e) => { if (e.Property == RangeBase.ValueProperty) OnCustomizeOffsetChanged('x', (double)e.NewValue!); };
                if (ySlider != null)
                    ySlider.PropertyChanged += (_, e) => { if (e.Property == RangeBase.ValueProperty) OnCustomizeOffsetChanged('y', (double)e.NewValue!); };
                if (zSlider != null)
                    zSlider.PropertyChanged += (_, e) => { if (e.Property == RangeBase.ValueProperty) OnCustomizeOffsetChanged('z', (double)e.NewValue!); };
                _customizeSlidersWired = true;
            }

            if (scaleRow != null) scaleRow.IsVisible = item.SupportsScale;
            if (xRow != null) xRow.IsVisible = item.SupportsOffset;
            if (yRow != null) yRow.IsVisible = item.SupportsOffset;
            if (zRow != null) zRow.IsVisible = item.SupportsOffset;

            if (scaleSlider != null) scaleSlider.Value = _customizingScale;
            if (xSlider != null) xSlider.Value = _customizingOffsetX;
            if (ySlider != null) ySlider.Value = _customizingOffsetY;
            if (zSlider != null) zSlider.Value = _customizingOffsetZ;
            UpdateCustomizeSliderLabels();
        }

        private void BuildColorSwatches(WrapPanel panel)
        {
            panel.Children.Clear();
            foreach (var color in CustomizeColorOrder)
            {
                var swatch = new Border
                {
                    Width = 26, Height = 26,
                    CornerRadius = new CornerRadius(13),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    BorderThickness = new Thickness(color == _customizingColor ? 2.5 : 1),
                    BorderBrush = color == _customizingColor ? Brushes.White : new SolidColorBrush(Color.Parse("#2A3848")),
                };
                if (color == "neutral")
                {
                    var grad = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    };
                    grad.GradientStops.Add(new GradientStop(Color.Parse("#1A1A1A"), 0.33));
                    grad.GradientStops.Add(new GradientStop(Color.Parse("#888888"), 0.66));
                    grad.GradientStops.Add(new GradientStop(Color.Parse("#F0F0F0"), 1.0));
                    swatch.Background = grad;
                }
                else
                {
                    swatch.Background = new SolidColorBrush(Color.Parse(CustomizeColorHex[color]));
                }
                var capturedColor = color;
                swatch.Tapped += (_, _) => OnCustomizeColorTapped(capturedColor);
                panel.Children.Add(swatch);
            }
        }

        private void BuildShadeRow(StackPanel row)
        {
            row.Children.Clear();
            var order = _customizingColor == "neutral" ? CustomizeNeutralOrder : CustomizeShadeOrder;
            if (!order.Contains(_customizingShade))
                _customizingShade = order[Math.Min(1, order.Length - 1)];
            foreach (var shade in order)
            {
                var btn = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(11, 5),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Background = shade == _customizingShade ? new SolidColorBrush(Color.FromArgb(0x2E, 0x65, 0xDD, 0x6D)) : new SolidColorBrush(Color.Parse("#0F1A24")),
                    BorderBrush = shade == _customizingShade ? new SolidColorBrush(Color.Parse("#65DD6D")) : new SolidColorBrush(Color.Parse("#1C2A38")),
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = char.ToUpper(shade[0]) + shade.Substring(1),
                        Foreground = shade == _customizingShade ? new SolidColorBrush(Color.Parse("#65DD6D")) : new SolidColorBrush(Color.Parse("#C0D0E0")),
                        FontSize = 11,
                        FontWeight = FontWeight.Medium,
                    },
                };
                var capturedShade = shade;
                btn.Tapped += (_, _) => OnCustomizeShadeTapped(capturedShade);
                row.Children.Add(btn);
            }
        }

        private void OnCustomizeColorTapped(string color)
        {
            if (_customizingColor == color) return;
            bool wasNeutral = _customizingColor == "neutral";
            bool isNeutral = color == "neutral";
            _customizingColor = color;
            var panel = this.FindControl<WrapPanel>("CustomizeColorSwatchesPanel");
            if (panel != null) BuildColorSwatches(panel);
            if (wasNeutral != isNeutral)
            {
                var shadeRow = this.FindControl<StackPanel>("CustomizeShadeRow");
                if (shadeRow != null) BuildShadeRow(shadeRow);
            }
        }

        private void OnCustomizeShadeTapped(string shade)
        {
            if (_customizingShade == shade) return;
            _customizingShade = shade;
            var shadeRow = this.FindControl<StackPanel>("CustomizeShadeRow");
            if (shadeRow != null) BuildShadeRow(shadeRow);
        }

        private void OnCustomizeScaleChanged(double v)
        {
            _customizingScale = Math.Round(Math.Clamp(v, 0.7, 1.3), 2);
            UpdateCustomizeSliderLabels();
        }

        private void OnCustomizeOffsetChanged(char axis, double v)
        {
            var clamped = Math.Round(Math.Clamp(v, -2.0, 2.0), 1);
            if (axis == 'x') _customizingOffsetX = clamped;
            else if (axis == 'y') _customizingOffsetY = clamped;
            else if (axis == 'z') _customizingOffsetZ = clamped;
            UpdateCustomizeSliderLabels();
        }

        private void UpdateCustomizeSliderLabels()
        {
            var sV = this.FindControl<TextBlock>("CustomizeScaleValue");
            var xV = this.FindControl<TextBlock>("CustomizeOffsetXValue");
            var yV = this.FindControl<TextBlock>("CustomizeOffsetYValue");
            var zV = this.FindControl<TextBlock>("CustomizeOffsetZValue");
            if (sV != null) sV.Text = _customizingScale.ToString("0.00") + "×";
            if (xV != null) xV.Text = (_customizingOffsetX >= 0 ? "+" : "") + _customizingOffsetX.ToString("0.0");
            if (yV != null) yV.Text = (_customizingOffsetY >= 0 ? "+" : "") + _customizingOffsetY.ToString("0.0");
            if (zV != null) zV.Text = (_customizingOffsetZ >= 0 ? "+" : "") + _customizingOffsetZ.ToString("0.0");
        }

        private void OnCustomizeResetTapped(object? sender, RoutedEventArgs e)
        {
            _customizingScale = 1.0;
            _customizingOffsetX = 0; _customizingOffsetY = 0; _customizingOffsetZ = 0;
            var scaleSlider = this.FindControl<Slider>("CustomizeScaleSlider");
            var xSlider = this.FindControl<Slider>("CustomizeOffsetXSlider");
            var ySlider = this.FindControl<Slider>("CustomizeOffsetYSlider");
            var zSlider = this.FindControl<Slider>("CustomizeOffsetZSlider");
            if (scaleSlider != null) scaleSlider.Value = 1.0;
            if (xSlider != null) xSlider.Value = 0;
            if (ySlider != null) ySlider.Value = 0;
            if (zSlider != null) zSlider.Value = 0;
            UpdateCustomizeSliderLabels();
        }

        private void OnCustomizeBackdropTapped(object? sender, TappedEventArgs e)
        {
            _ = CloseCustomizeOverlayAsync();
        }

        private void OnCustomizeClose(object? sender, TappedEventArgs e)
        {
            _ = CloseCustomizeOverlayAsync();
        }

        private void OnCustomizeCancel(object? sender, TappedEventArgs e)
        {
            _ = CloseCustomizeOverlayAsync();
        }

        private async void OnCustomizeApply(object? sender, TappedEventArgs e)
        {
            var item = _customizingCosmetic;
            if (item == null) { _ = CloseCustomizeOverlayAsync(); return; }

            string? variantSlug = null;
            if (item.SupportsVariants && item.Variants != null)
            {
                variantSlug = ComputeVariantSlug(_customizingColor, _customizingShade);
                if (item.Variants.FirstOrDefault(v => v.Slug == variantSlug) == null) variantSlug = null;
            }
            double? scaleArg = item.SupportsScale ? _customizingScale : null;
            double? oxArg = item.SupportsOffset ? _customizingOffsetX : null;
            double? oyArg = item.SupportsOffset ? _customizingOffsetY : null;
            double? ozArg = item.SupportsOffset ? _customizingOffsetZ : null;

            try
            {
                var token = _currentSettings?.LeafApiJwt;
                if (!string.IsNullOrEmpty(token))
                {
                    var resp = await LeafApiService.CustomizeCosmeticAsync(token, item.Id, variantSlug, scaleArg, oxArg, oyArg, ozArg);
                    if (resp == null)
                    {
                        LeafLog.Info("Customize", $"server rejected for {item.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("Customize", $"apply error: {ex.Message}");
            }

            WriteEquippedCustomization(item.Category, variantSlug, scaleArg, oxArg, oyArg, ozArg);
            try { if (_currentSettings != null) await _settingsService.SaveSettingsAsync(_currentSettings); } catch { }
            try { WriteEquippedJson(_currentSettings?.Equipped, _currentSettings?.Equipped?.Sub); } catch { }

            try
            {
                try { LeafClient.Services.CosmeticHelpers.InvalidateCardPreviewCache(); } catch { }
                try
                {
                    var page = this.FindControl<LeafClient.Views.Pages.CosmeticsPageView>("CosmeticsPage");
                    if (page != null)
                    {
                        await page.RefreshRendererPublicAsync();
                        page.PopulateCosmeticsGridPublic();
                    }
                }
                catch (Exception ex2) { LeafLog.Info("Customize", $"page refresh failed: {ex2.Message}"); }
            }
            catch (Exception ex) { LeafLog.Info("Customize", $"refresh failed: {ex.Message}"); }

            await CloseCustomizeOverlayAsync();
        }

        private static string ComputeVariantSlug(string color, string shade)
        {
            if (color == "neutral") return shade;
            if (shade == "normal") return color;
            return $"{shade}-{color}";
        }

        private string? ReadEquippedVariantFor(string category)
        {
            var e = _currentSettings?.Equipped;
            if (e == null) return null;
            return category switch
            {
                "cape" => e.CapeVariant,
                "hat" => e.HatVariant,
                "wings" => e.WingsVariant,
                "aura" => e.AuraVariant,
                "face" => e.FaceVariant,
                _ => null,
            };
        }

        private double? ReadEquippedScaleFor(string category)
        {
            var e = _currentSettings?.Equipped;
            if (e == null) return null;
            return category switch
            {
                "cape" => e.CapeScale,
                "hat" => e.HatScale,
                "wings" => e.WingsScale,
                "aura" => e.AuraScale,
                "face" => e.FaceScale,
                _ => null,
            };
        }

        private double? ReadEquippedOffsetFor(string category, char axis)
        {
            var e = _currentSettings?.Equipped;
            if (e == null) return null;
            return (category, axis) switch
            {
                ("cape", 'x') => e.CapeOffsetX, ("cape", 'y') => e.CapeOffsetY, ("cape", 'z') => e.CapeOffsetZ,
                ("hat", 'x') => e.HatOffsetX, ("hat", 'y') => e.HatOffsetY, ("hat", 'z') => e.HatOffsetZ,
                ("wings", 'x') => e.WingsOffsetX, ("wings", 'y') => e.WingsOffsetY, ("wings", 'z') => e.WingsOffsetZ,
                ("aura", 'x') => e.AuraOffsetX, ("aura", 'y') => e.AuraOffsetY, ("aura", 'z') => e.AuraOffsetZ,
                ("face", 'x') => e.FaceOffsetX, ("face", 'y') => e.FaceOffsetY, ("face", 'z') => e.FaceOffsetZ,
                _ => null,
            };
        }

        private void WriteEquippedCustomization(string category, string? variant, double? scale, double? ox, double? oy, double? oz)
        {
            if (_currentSettings == null) return;
            _currentSettings.Equipped ??= new Models.EquippedCosmetics();
            var e = _currentSettings.Equipped;
            switch (category)
            {
                case "cape":
                    e.CapeVariant = variant; e.CapeScale = scale;
                    e.CapeOffsetX = ox; e.CapeOffsetY = oy; e.CapeOffsetZ = oz;
                    break;
                case "hat":
                    e.HatVariant = variant; e.HatScale = scale;
                    e.HatOffsetX = ox; e.HatOffsetY = oy; e.HatOffsetZ = oz;
                    break;
                case "wings":
                    e.WingsVariant = variant; e.WingsScale = scale;
                    e.WingsOffsetX = ox; e.WingsOffsetY = oy; e.WingsOffsetZ = oz;
                    break;
                case "aura":
                    e.AuraVariant = variant; e.AuraScale = scale;
                    e.AuraOffsetX = ox; e.AuraOffsetY = oy; e.AuraOffsetZ = oz;
                    break;
                case "face":
                    e.FaceVariant = variant; e.FaceScale = scale;
                    e.FaceOffsetX = ox; e.FaceOffsetY = oy; e.FaceOffsetZ = oz;
                    break;
            }
        }

        private async Task OpenCustomizeOverlayAsync()
        {
            var overlay = this.FindControl<Grid>("CustomizeCosmeticOverlay");
            var panel = this.FindControl<Border>("CustomizePanel");
            var backdrop = this.FindControl<Border>("CustomizeBackdrop");
            if (overlay == null || panel == null) return;
            if (_customizeAnimating || overlay.IsVisible) return;
            _customizeAnimating = true;

            overlay.IsVisible = true;
            if (backdrop != null) backdrop.Opacity = 0;
            panel.Opacity = 0;
            var st = panel.RenderTransform as ScaleTransform ?? new ScaleTransform(0.85, 0.85);
            panel.RenderTransform = st;
            panel.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            st.ScaleX = 0.85; st.ScaleY = 0.85;

            const int steps = 14;
            const int durationMs = 200;
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double e = 1 - Math.Pow(1 - t, 3);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (backdrop != null) backdrop.Opacity = e;
                    panel.Opacity = e;
                    st.ScaleX = 0.85 + 0.15 * e;
                    st.ScaleY = 0.85 + 0.15 * e;
                });
                if (i < steps) await Task.Delay(durationMs / steps);
            }
            _customizeAnimating = false;
        }

        private async Task CloseCustomizeOverlayAsync()
        {
            var overlay = this.FindControl<Grid>("CustomizeCosmeticOverlay");
            var panel = this.FindControl<Border>("CustomizePanel");
            var backdrop = this.FindControl<Border>("CustomizeBackdrop");
            if (overlay == null || !overlay.IsVisible || _customizeAnimating) return;
            _customizeAnimating = true;
            var st = panel?.RenderTransform as ScaleTransform;
            const int steps = 10;
            const int durationMs = 140;
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (backdrop != null) backdrop.Opacity = 1 - t;
                    if (panel != null) panel.Opacity = 1 - t;
                    if (st != null) { st.ScaleX = 1 - 0.1 * t; st.ScaleY = 1 - 0.1 * t; }
                });
                if (i < steps) await Task.Delay(durationMs / steps);
            }
            overlay.IsVisible = false;
            _customizingCosmetic = null;
            _customizeAnimating = false;
        }
    }
}
