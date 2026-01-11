using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeafClientUpdater.Views
{
    public partial class UpdaterWindow : Window
    {
        private TextBlock? _statusText;
        private Border? _spinnerBorder;
        private Button? _closeButton;
        private Canvas? _particleCanvas;
        private Border? _backgroundBorder;

        private bool _isSpinnerAnimating = true;
        private readonly List<Particle> _particles = new();
        private readonly Random _rand = new();
        private CancellationTokenSource? _animationCts;
        private double _rotationAngle = 0;
        private double _currentHue = 0;

        public UpdaterWindow()
        {
            InitializeComponent();

            _statusText = this.FindControl<TextBlock>("StatusText");
            _spinnerBorder = this.FindControl<Border>("SpinnerBorder");
            _closeButton = this.FindControl<Button>("CloseButton");
            _particleCanvas = this.FindControl<Canvas>("ParticleCanvas");
            _backgroundBorder = this.FindControl<Border>("BackgroundBorder");

            if (_closeButton != null)
                _closeButton.Click += (_, __) => Close();

            StartAnimations();

            this.Opened += async (s, e) => await StartUpdateProcess();
        }

        private void OnDragWindow(object? sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }

        private void StartAnimations()
        {
            _animationCts?.Cancel();
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;

            _particles.Clear();
            _particleCanvas?.Children.Clear();
            for (int i = 0; i < 60; i++) SpawnParticle();

            // Run animation loop on background thread to prevent pausing during window drag
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Dispatcher.UIThread.InvokeAsync(UpdateVisuals, DispatcherPriority.Render);
                        await Task.Delay(16, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        protected override void OnClosed(EventArgs e)
        {
            _animationCts?.Cancel();
            base.OnClosed(e);
        }


        private void UpdateVisuals()
        {
            if (_isSpinnerAnimating && _spinnerBorder != null)
            {
                _rotationAngle += 15;
                if (_rotationAngle >= 360) _rotationAngle = 0;
                _spinnerBorder.RenderTransform = new RotateTransform(_rotationAngle);
            }

            if (_backgroundBorder != null && _backgroundBorder.Background is LinearGradientBrush gradient)
            {
                _currentHue += 0.5;
                if (_currentHue > 360) _currentHue = 0;

                var color = ColorFromHsv(_currentHue, 0.8, 0.2);

                if (gradient.GradientStops.Count > 0)
                {
                    gradient.GradientStops[gradient.GradientStops.Count - 1].Color = color;
                }
            }

            if (_particleCanvas != null)
            {
                for (int i = _particles.Count - 1; i >= 0; i--)
                {
                    var p = _particles[i];
                    p.Y -= p.Speed;
                    p.X += Math.Sin((p.Y + p.Seed) / 20.0) * 0.5;
                    p.Opacity -= 0.008;

                    if (p.Y < -10 || p.Opacity <= 0)
                    {
                        _particleCanvas.Children.Remove(p.Element);
                        _particles.RemoveAt(i);
                        SpawnParticle();
                    }
                    else
                    {
                        Canvas.SetTop(p.Element, p.Y);
                        Canvas.SetLeft(p.Element, p.X);
                        p.Element.Opacity = p.Opacity;
                    }
                }
            }
        }

        private void SpawnParticle()
        {
            if (_particleCanvas == null) return;

            double size = _rand.NextDouble() > 0.8 ? _rand.Next(3, 6) : _rand.Next(1, 3);

            var ellipse = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = Brushes.White,
                Opacity = _rand.NextDouble() * 0.4 + 0.1
            };

            double width = 400 - (30 * 2);
            double height = 500 - (30 * 2);

            double x = _rand.NextDouble() * width;
            double y = height + _rand.NextDouble() * 50;

            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);

            _particleCanvas.Children.Add(ellipse);
            _particles.Add(new Particle
            {
                Element = ellipse,
                X = x,
                Y = y,
                Speed = _rand.NextDouble() * 2.0 + 1.0,
                Opacity = ellipse.Opacity,
                Seed = _rand.Next(0, 100)
            });
        }

        private Color ColorFromHsv(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - f * saturation));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            if (hi == 0) return Color.FromRgb(v, t, p);
            else if (hi == 1) return Color.FromRgb(q, v, p);
            else if (hi == 2) return Color.FromRgb(p, v, t);
            else if (hi == 3) return Color.FromRgb(p, q, v);
            else if (hi == 4) return Color.FromRgb(t, p, v);
            else return Color.FromRgb(v, p, q);
        }

        private class Particle
        {
            public Ellipse Element { get; set; } = null!;
            public double X { get; set; }
            public double Y { get; set; }
            public double Speed { get; set; }
            public double Opacity { get; set; }
            public int Seed { get; set; }
        }

        private async Task StartUpdateProcess()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length < 4)
            {
                ShowError("Invalid arguments passed to updater.");
                return;
            }

            try
            {
                int parentPid = int.Parse(args[1]);
                string targetExePath = Encoding.UTF8.GetString(Convert.FromBase64String(args[2]));
                string downloadUrl = Encoding.UTF8.GetString(Convert.FromBase64String(args[3]));

                UpdateStatus("Waiting for Leaf Client to close...");

                try
                {
                    var parentProc = Process.GetProcessById(parentPid);
                    if (parentProc != null && !parentProc.HasExited)
                    {
                        await parentProc.WaitForExitAsync();
                    }
                }
                catch (ArgumentException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Wait error: {ex.Message}");
                }

                await Task.Delay(1000);

                string appDir = System.IO.Path.GetDirectoryName(targetExePath)!;
                string tempFilePath = System.IO.Path.Combine(appDir, "LeafClient.new");
                string backupPath = targetExePath + ".bak";

                UpdateStatus("Downloading latest version...");
                using (var client = new HttpClient())
                {
                    var data = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(tempFilePath, data);
                }

                UpdateStatus("Installing update...");

                if (File.Exists(backupPath)) File.Delete(backupPath);

                if (File.Exists(targetExePath)) File.Move(targetExePath, backupPath);

                File.Move(tempFilePath, targetExePath);

                UpdateStatus("Update Complete! Launching...");
                await Task.Delay(1000);

                Process.Start(new ProcessStartInfo
                {
                    FileName = targetExePath,
                    UseShellExecute = true
                });

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                ShowError($"Update Failed: {ex.Message}");
            }
        }

        private void UpdateStatus(string text)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_statusText != null) _statusText.Text = text;
            });
        }

        private void ShowError(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _isSpinnerAnimating = false;
                if (_statusText != null)
                {
                    _statusText.Text = message;
                    _statusText.Foreground = Brushes.Red;
                }
                if (_spinnerBorder != null)
                {
                    _spinnerBorder.BorderBrush = Brushes.Red;
                }
                if (_closeButton != null)
                {
                    _closeButton.IsVisible = true;
                }
            });
        }
    }
}
