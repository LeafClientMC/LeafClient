using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public static class SmoothScroll
    {
        private const double WheelPixelsPerNotch = 90.0;

        private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(240);

        private static readonly TimeSpan FrameTime = TimeSpan.FromMilliseconds(16);

        public static readonly AttachedProperty<bool> IsEnabledProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, bool>(
                "IsEnabled", typeof(SmoothScroll));

        public static bool GetIsEnabled(ScrollViewer sv) => sv.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(ScrollViewer sv, bool value) => sv.SetValue(IsEnabledProperty, value);

        private static readonly ConditionalWeakTable<ScrollViewer, AnimationState> _state = new();

        private sealed class AnimationState
        {
            public CancellationTokenSource? Cts;
            public double TargetY;
            public bool HasTarget;
        }

        static SmoothScroll()
        {
            IsEnabledProperty.Changed.AddClassHandler<ScrollViewer>(OnIsEnabledChanged);
        }

        private static void OnIsEnabledChanged(ScrollViewer sv, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
            {
                sv.AddHandler(InputElement.PointerWheelChangedEvent, OnWheel,
                    RoutingStrategies.Tunnel, handledEventsToo: false);
            }
            else
            {
                sv.RemoveHandler(InputElement.PointerWheelChangedEvent, OnWheel);
            }
        }

        private static async void OnWheel(object? sender, PointerWheelEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;

            double wheelDelta = e.Delta.Y;
            if (wheelDelta == 0) return;

            double maxY = Math.Max(0, sv.Extent.Height - sv.Viewport.Height);
            if (maxY <= 0) return;

            e.Handled = true;

            var state = _state.GetValue(sv, _ => new AnimationState());

            double baseY = state.HasTarget ? state.TargetY : sv.Offset.Y;
            double targetY = Math.Clamp(baseY - wheelDelta * WheelPixelsPerNotch, 0, maxY);

            state.TargetY  = targetY;
            state.HasTarget = true;

            state.Cts?.Cancel();
            state.Cts = new CancellationTokenSource();
            var token = state.Cts.Token;

            double startY = sv.Offset.Y;
            double deltaY = targetY - startY;
            if (Math.Abs(deltaY) < 0.5)
            {
                state.HasTarget = false;
                return;
            }

            var start = DateTime.UtcNow;
            try
            {
                while (true)
                {
                    if (token.IsCancellationRequested) return;

                    var elapsed = DateTime.UtcNow - start;
                    double t = Math.Clamp(elapsed.TotalMilliseconds / Duration.TotalMilliseconds, 0, 1);
                    double eased = 1 - Math.Pow(1 - t, 3);

                    double y = startY + deltaY * eased;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        var current = sv.Offset;
                        sv.Offset = new Vector(current.X, y);
                    });

                    if (t >= 1) break;
                    await Task.Delay(FrameTime, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (state.Cts?.Token == token)
                {
                    state.HasTarget = false;
                }
            }
        }
    }
}
