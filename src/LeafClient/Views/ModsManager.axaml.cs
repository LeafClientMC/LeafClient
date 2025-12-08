using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LeafClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeafClient.Views
{
    public partial class ModsManager : Window
    {
        private int _currentIndex = 1;
        private double _targetOffset = 0;
        private const double ItemWidth = 280;
        private const double ItemMargin = 100;
        private const double TotalItemStride = ItemWidth + ItemMargin;
        private DispatcherTimer _animationTimer;
        private IBrush? _carouselMask;
        private Canvas? _particleLayer;
        private CancellationTokenSource? _particleCts;
        private readonly Random _rand = new();
        private readonly List<Particle> _particles = new();

        private struct Particle
        {
            public Ellipse Shape; public double X; public double Y; public double Speed; public double Drift;
        }

        public ModsManager()
        {
            InitializeComponent();
            DataContext = new ModsManagerViewModel();

            var contentGrid = this.FindControl<Grid>("ContentGrid");
            _carouselMask = contentGrid?.OpacityMask;

            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _animationTimer.Tick += AnimationTimer_Tick;

            _particleLayer = this.FindControl<Canvas>("ParticleLayer");
            SetupParticles();

            this.Opened += ModsManager_Opened;
            this.Closed += ModsManager_Closed;
            this.Deactivated += (s, e) => Close();
        }

        private void ModsManager_Opened(object? sender, EventArgs e)
        {
            var scrollViewer = this.FindControl<ScrollViewer>("ModScrollViewer");
            if (scrollViewer == null) return;

            _targetOffset = _currentIndex * TotalItemStride;
            scrollViewer.Offset = new Vector(_targetOffset, 0);
            UpdateCardScales(scrollViewer);
        }

        private void ModsManager_Closed(object? sender, EventArgs e)
        {
            _particleCts?.Cancel();
            _particleCts?.Dispose();
        }

        private void SwitchToMods(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.FindControl<ScrollViewer>("ModScrollViewer") is { } mods) mods.IsVisible = true;
            if (this.FindControl<Grid>("WaypointsView") is { } waypoints) waypoints.IsVisible = false;
            if (this.FindControl<Grid>("SettingsView") is { } settings) settings.IsVisible = false;

            if (this.FindControl<Grid>("PerformanceHeader") is { } header) header.IsVisible = true;
            if (this.FindControl<Grid>("ContentGrid") is { } grid) grid.OpacityMask = _carouselMask;

            UpdateNavButtons("MODS");
        }

        private void SwitchToWaypoints(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.FindControl<ScrollViewer>("ModScrollViewer") is { } mods) mods.IsVisible = false;
            if (this.FindControl<Grid>("WaypointsView") is { } waypoints) waypoints.IsVisible = true;
            if (this.FindControl<Grid>("SettingsView") is { } settings) settings.IsVisible = false;

            if (this.FindControl<Grid>("PerformanceHeader") is { } header) header.IsVisible = false;
            if (this.FindControl<Grid>("ContentGrid") is { } grid) grid.OpacityMask = null;

            UpdateNavButtons("WAYPOINTS");
        }

        private void SwitchToSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.FindControl<ScrollViewer>("ModScrollViewer") is { } mods) mods.IsVisible = false;
            if (this.FindControl<Grid>("WaypointsView") is { } waypoints) waypoints.IsVisible = false;
            if (this.FindControl<Grid>("SettingsView") is { } settings) settings.IsVisible = true;

            if (this.FindControl<Grid>("PerformanceHeader") is { } header) header.IsVisible = false;
            if (this.FindControl<Grid>("ContentGrid") is { } grid) grid.OpacityMask = null;

            UpdateNavButtons("SETTINGS");
        }

        private void UpdateNavButtons(string activeTab)
        {
            var btnMods = this.FindControl<Button>("BtnMods");
            var btnWaypoints = this.FindControl<Button>("BtnWaypoints");
            var btnSettings = this.FindControl<Button>("BtnSettings");

            if (btnMods == null || btnWaypoints == null || btnSettings == null) return;

            btnMods.Classes.Remove("Selected");
            btnWaypoints.Classes.Remove("Selected");
            btnSettings.Classes.Remove("Selected");

            if (activeTab == "MODS") btnMods.Classes.Add("Selected");
            else if (activeTab == "WAYPOINTS") btnWaypoints.Classes.Add("Selected");
            else if (activeTab == "SETTINGS") btnSettings.Classes.Add("Selected");
        }

        private void SetupParticles()
        {
            if (_particleLayer == null) return;
            if (_particleLayer.Bounds.Width <= 0 || _particleLayer.Bounds.Height <= 0)
            {
                _particleLayer.LayoutUpdated += ParticleLayer_LayoutUpdated;
                return;
            }
            CreateParticles();
        }

        private void ParticleLayer_LayoutUpdated(object? sender, EventArgs e)
        {
            if (_particleLayer == null) return;
            if (_particleLayer.Bounds.Width > 0 && _particleLayer.Bounds.Height > 0)
            {
                _particleLayer.LayoutUpdated -= ParticleLayer_LayoutUpdated;
                CreateParticles();
            }
        }

        private void CreateParticles()
        {
            if (_particleLayer == null) return;

            _particleCts?.Cancel();
            _particles.Clear();
            _particleLayer.Children.Clear();

            double w = _particleLayer.Bounds.Width;
            double h = _particleLayer.Bounds.Height;

            for (int i = 0; i < 40; i++)
            {
                var size = _rand.Next(2, 5);
                var star = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    Opacity = 0.2 + _rand.NextDouble() * 0.4
                };

                double x = _rand.NextDouble() * w;
                double y = _rand.NextDouble() * h;

                Canvas.SetLeft(star, x);
                Canvas.SetTop(star, y);
                _particleLayer.Children.Add(star);

                _particles.Add(new Particle
                {
                    Shape = star,
                    X = x,
                    Y = y,
                    Speed = 0.2 + _rand.NextDouble() * 0.5,
                    Drift = (_rand.NextDouble() - 0.5) * 0.2
                });
            }

            _particleCts = new CancellationTokenSource();
            var ct = _particleCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (_particleLayer == null) return;
                            double currentW = _particleLayer.Bounds.Width;
                            double currentH = _particleLayer.Bounds.Height;

                            for (int i = 0; i < _particles.Count; i++)
                            {
                                var p = _particles[i];
                                p.Y -= p.Speed;
                                p.X += p.Drift;

                                if (p.Y < -10 || p.X < -10 || p.X > currentW + 10)
                                {
                                    p.Y = currentH + 10;
                                    p.X = _rand.NextDouble() * currentW;
                                }

                                Canvas.SetLeft(p.Shape, p.X);
                                Canvas.SetTop(p.Shape, p.Y);
                                _particles[i] = p;
                            }
                        });
                        await Task.Delay(33, ct);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception) { }
            }, ct);
        }

        public void CloseButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

        private void ScrollViewer_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;

            var viewModel = DataContext as ModsManagerViewModel;
            if (viewModel == null) return;

            if (e.Delta.Y < 0)
            {
                if (_currentIndex < viewModel.FilteredMods.Count - 1)
                    _currentIndex++;
            }
            else if (e.Delta.Y > 0)
            {
                if (_currentIndex > 0)
                    _currentIndex--;
            }

            _targetOffset = _currentIndex * TotalItemStride;

            if (!_animationTimer.IsEnabled)
                _animationTimer.Start();

            e.Handled = true;
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            var scrollViewer = this.FindControl<ScrollViewer>("ModScrollViewer");
            if (scrollViewer != null)
            {
                double currentOffset = scrollViewer.Offset.X;
                double newOffset = currentOffset + (_targetOffset - currentOffset) * 0.15;

                if (Math.Abs(_targetOffset - newOffset) < 0.5)
                {
                    newOffset = _targetOffset;
                    _animationTimer.Stop();
                }
                scrollViewer.Offset = new Vector(newOffset, scrollViewer.Offset.Y);
                UpdateCardScales(scrollViewer);
            }
        }

        private void Slider_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            if (DataContext is ModsManagerViewModel vm)
            {
                vm.SaveGeneralSettingsCommand.Execute(null);
            }
        }

        private void EditHudLayoutButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string signalPath = System.IO.Path.Combine(appData, ".minecraft", "leaf_open_hud_editor.signal");
                System.IO.File.Create(signalPath).Close();
                this.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error triggering HUD editor: {ex.Message}");
            }
        }

        private void UpdateCardScales(ScrollViewer scrollViewer)
        {
            var presenter = scrollViewer.GetVisualDescendants().OfType<ItemsPresenter>().FirstOrDefault();
            var panel = presenter?.Panel as Panel;

            if (panel == null) return;

            double centerScreen = scrollViewer.Bounds.Width / 2;
            double currentScroll = scrollViewer.Offset.X;

            foreach (var child in panel.Children)
            {
                if (child is Visual visualChild)
                {
                    double childCenter = visualChild.Bounds.Center.X - currentScroll + 310;
                    double dist = Math.Abs(centerScreen - childCenter);
                    double scale = 1.0;

                    if (dist < 400)
                    {
                        double factor = 1 - (dist / 400);
                        scale = 0.9 + (factor * 0.25);
                    }
                    else
                    {
                        scale = 0.9;
                    }

                    visualChild.RenderTransform = new ScaleTransform(scale, scale);
                    visualChild.ZIndex = scale > 1.0 ? 10 : 0;
                }
            }
        }
    }
}
