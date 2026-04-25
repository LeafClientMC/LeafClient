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
    /// <summary>
    /// Attached-property helper that turns the default instant mouse-wheel
    /// scrolling of a <see cref="ScrollViewer"/> into a smooth, eased animation
    /// of its <see cref="ScrollViewer.Offset"/> property.  Applied globally via
    /// a style in App.axaml so every ScrollViewer in the launcher inherits the
    /// behaviour without per-control wiring.
    /// </summary>
    public static class SmoothScroll
    {
        /// <summary>Pixels scrolled per wheel notch.</summary>
        private const double WheelPixelsPerNotch = 90.0;

        /// <summary>Animation duration.</summary>
        private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(240);

        /// <summary>UI tick frequency.</summary>
        private static readonly TimeSpan FrameTime = TimeSpan.FromMilliseconds(16);

        public static readonly AttachedProperty<bool> IsEnabledProperty =
            AvaloniaProperty.RegisterAttached<ScrollViewer, bool>(
                "IsEnabled", typeof(SmoothScroll));

        public static bool GetIsEnabled(ScrollViewer sv) => sv.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(ScrollViewer sv, bool value) => sv.SetValue(IsEnabledProperty, value);

        // Per-ScrollViewer animation state, keyed weakly so GC'd controls don't leak.
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
                // Tunnel so we catch the wheel event before the ScrollViewer's
                // default handler can process it (and consume our chance to
                // replace the instant scroll with an animated one).
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

            // Respect nested scrollables — if the inner scrollable can still
            // scroll in this direction, let it eat the event instead.  For the
            // common case (single ScrollViewer on a page) we just take it.
            double wheelDelta = e.Delta.Y;
            if (wheelDelta == 0) return;

            double maxY = Math.Max(0, sv.Extent.Height - sv.Viewport.Height);
            if (maxY <= 0) return; // nothing to scroll

            e.Handled = true;

            var state = _state.GetValue(sv, _ => new AnimationState());

            // If we already have a target from a previous wheel tick that
            // hasn't finished, chain the new delta onto it so repeated spins
            // of the wheel accumulate instead of resetting to the current
            // offset each time.
            double baseY = state.HasTarget ? state.TargetY : sv.Offset.Y;
            double targetY = Math.Clamp(baseY - wheelDelta * WheelPixelsPerNotch, 0, maxY);

            state.TargetY  = targetY;
            state.HasTarget = true;

            // Cancel the previous animation so a new one can start from the
            // current offset toward the (possibly updated) target.
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
                    double eased = 1 - Math.Pow(1 - t, 3); // ease-out cubic

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
                // new wheel tick arrived — fine, it started its own animation
            }
            finally
            {
                // Only clear the target if *we* are still the active animation
                // (another wheel tick may have replaced state.Cts already).
                if (state.Cts?.Token == token)
                {
                    state.HasTarget = false;
                }
            }
        }
    }
}
