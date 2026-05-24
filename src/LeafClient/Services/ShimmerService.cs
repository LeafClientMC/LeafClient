using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace LeafClient.Services
{
    public static class ShimmerService
    {
        private sealed class ShimmerHandle
        {
            public DispatcherTimer Timer = null!;
            public IBrush? OriginalBackground;
            public LinearGradientBrush Brush = null!;
            public double Phase;
        }

        private static readonly Dictionary<Border, ShimmerHandle> _active = new();

        private static readonly Color BaseColor      = Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF);
        private static readonly Color HighlightColor = Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF);

        public static void Start(Border? target)
        {
            if (target == null) return;
            Stop(target);

            var brush = new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0.5, Avalonia.RelativeUnit.Relative),
                EndPoint   = new Avalonia.RelativePoint(1, 0.5, Avalonia.RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(BaseColor,      0.0),
                    new GradientStop(BaseColor,      0.40),
                    new GradientStop(HighlightColor, 0.50),
                    new GradientStop(BaseColor,      0.60),
                    new GradientStop(BaseColor,      1.0),
                },
            };

            var handle = new ShimmerHandle
            {
                OriginalBackground = target.Background,
                Brush              = brush,
                Phase              = -0.5,
            };

            target.Background = brush;

            handle.Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            handle.Timer.Tick += (_, __) =>
            {
                handle.Phase += 0.025;
                if (handle.Phase > 1.5) handle.Phase = -0.5;
                double c = handle.Phase;
                brush.GradientStops[0].Offset = Math.Max(0, c - 0.5);
                brush.GradientStops[1].Offset = Math.Max(0, Math.Min(1, c - 0.10));
                brush.GradientStops[2].Offset = Math.Max(0, Math.Min(1, c));
                brush.GradientStops[3].Offset = Math.Max(0, Math.Min(1, c + 0.10));
                brush.GradientStops[4].Offset = Math.Min(1, c + 0.50);
            };
            handle.Timer.Start();

            _active[target] = handle;
        }

        public static void Stop(Border? target)
        {
            if (target == null) return;
            if (_active.TryGetValue(target, out var handle))
            {
                try { handle.Timer.Stop(); } catch { }
                target.Background = handle.OriginalBackground;
                _active.Remove(target);
            }
        }

        public static bool IsRunning(Border target) => _active.ContainsKey(target);
    }
}
