using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LeafClient.Models;
using LeafClient.Services;

namespace LeafClient.Views;

public partial class TutorialOverlay : UserControl
{
    private const double TooltipMaxWidth = 260.0;
    private const double TooltipEstimatedHeight = 120.0;
    private const double WelcomeCardWidth = 480.0;
    private const double WelcomeCardEstimatedHeight = 440.0;

    private bool _initialized;
    private MainWindow? _window;
    private Path? _backdropPath;
    private Border? _spotlightBorder;
    private Border? _tooltipCard;
    private TextBlock? _tooltipTitle;
    private TextBlock? _tooltipBody;
    private Button? _tooltipSkip;
    private Button? _tooltipNext;
    private Border? _welcomeCard;
    private TextBlock? _welcomeTitle;
    private TextBlock? _welcomeBody;
    private Button? _welcomeStartBtn;
    private TextBlock? _welcomeStartBtnText;

    private Control? _waitTarget;
    private EventHandler<PointerReleasedEventArgs>? _waitHandler;

    public TutorialOverlay()
    {
        InitializeComponent();
    }

    public void Initialize(MainWindow window)
    {
        if (_initialized) return;
        _initialized = true;
        _window = window;
        _backdropPath = this.FindControl<Path>("BackdropPath");
        _spotlightBorder = this.FindControl<Border>("SpotlightBorder");
        _tooltipCard = this.FindControl<Border>("TooltipCard");
        _tooltipTitle = this.FindControl<TextBlock>("TooltipTitle");
        _tooltipBody = this.FindControl<TextBlock>("TooltipBody");
        _tooltipSkip = this.FindControl<Button>("TooltipSkip");
        _tooltipNext = this.FindControl<Button>("TooltipNext");
        _welcomeCard = this.FindControl<Border>("WelcomeCard");
        _welcomeTitle = this.FindControl<TextBlock>("WelcomeTitle");
        _welcomeBody = this.FindControl<TextBlock>("WelcomeBody");
        _welcomeStartBtn = this.FindControl<Button>("WelcomeStartBtn");
        _welcomeStartBtnText = this.FindControl<TextBlock>("WelcomeStartBtnText");

        if (_welcomeStartBtn != null) _welcomeStartBtn.Click += (_, _) => TutorialService.Instance.Next();
        if (_tooltipSkip != null) _tooltipSkip.IsVisible = false;
        if (_tooltipNext != null) _tooltipNext.Click += (_, _) => TutorialService.Instance.Next();

        TutorialService.Instance.TutorialStarted += OnTutorialStarted;
        TutorialService.Instance.StepChanged += OnStepChanged;
        TutorialService.Instance.TutorialEnded += OnTutorialEnded;
        TutorialService.Instance.TutorialHidden += OnTutorialHidden;
        TutorialService.Instance.TutorialResumed += OnTutorialResumed;
    }

    private void OnTutorialStarted()
    {
        IsVisible = true;
    }

    private async void OnStepChanged(TutorialStep step)
    {
        DetachWaitHandler();

        bool hasDelay = step.NavigateToPage.HasValue || step.OpenAccountPanel || step.OnEnter != TutorialOnEnter.None;
        if (hasDelay)
        {
            IsVisible = false;
            if (step.NavigateToPage.HasValue || step.OpenAccountPanel)
                await Task.Delay(500);
            if (step.OnEnter != TutorialOnEnter.None)
                await Task.Delay(1000);
        }

        if (_window == null) return;

        if (step.CenterTooltip)
        {
            ShowWelcome(step);
            IsVisible = true;
            return;
        }

        if (_welcomeCard != null) _welcomeCard.IsVisible = false;
        if (_tooltipCard != null) _tooltipCard.IsVisible = true;

        Control? target = null;
        Point? pos = null;
        for (int i = 0; i < 20; i++)
        {
            target = FindControlByName(_window, step.TargetElementName);
            if (target != null)
            {
                pos = target.TranslatePoint(new Point(0, 0), this);
                if (pos != null && target.Bounds.Width > 0) break;
            }
            target = null;
            pos = null;
            await Task.Delay(100);
        }
        if (target == null || pos == null)
        {
            TutorialService.Instance.Next();
            return;
        }

        await Task.Delay(150);
        pos = target.TranslatePoint(new Point(0, 0), this) ?? pos;

        var x = pos.Value.X;
        var y = pos.Value.Y;
        var w = target.Bounds.Width;
        var h = target.Bounds.Height;

        bool isLargeContainer = (w > 400 || h > 400) && target is Border;
        if (isLargeContainer)
        {
            double minLeft = double.MaxValue, minTop = double.MaxValue;
            double maxRight = double.MinValue, maxBottom = double.MinValue;
            foreach (var v in target.GetVisualDescendants())
            {
                if (v is not Control ctrl) continue;
                if (!ctrl.IsVisible) continue;
                if (ctrl.Bounds.Width <= 0 || ctrl.Bounds.Height <= 0) continue;
                if (ctrl is Panel) continue;
                if (ctrl is ScrollViewer) continue;
                var tl = ctrl.TranslatePoint(new Point(0, 0), this);
                var br = ctrl.TranslatePoint(new Point(ctrl.Bounds.Width, ctrl.Bounds.Height), this);
                if (!tl.HasValue || !br.HasValue) continue;
                if (tl.Value.X < x - 20 || tl.Value.Y < y - 20) continue;
                if (br.Value.X > x + w + 20 || br.Value.Y > y + h + 20) continue;
                if (tl.Value.X < minLeft) minLeft = tl.Value.X;
                if (tl.Value.Y < minTop) minTop = tl.Value.Y;
                if (br.Value.X > maxRight) maxRight = br.Value.X;
                if (br.Value.Y > maxBottom) maxBottom = br.Value.Y;
            }
            if (minLeft != double.MaxValue && maxRight > minLeft && maxBottom > minTop)
            {
                const double contentPad = 16.0;
                double contentX = Math.Max(x, minLeft - contentPad);
                double contentY = Math.Max(y, minTop - contentPad);
                double contentRight = Math.Min(x + w, maxRight + contentPad);
                double contentBottom = Math.Min(y + h, maxBottom + contentPad);
                double contentW = contentRight - contentX;
                double contentH = contentBottom - contentY;
                if (contentH < h - 20 || contentW < w - 20)
                {
                    x = contentX;
                    y = contentY;
                    w = contentW;
                    h = contentH;
                }
            }
        }

        var padding = isLargeContainer ? 4.0 : 6.0;

        var spotX = x - padding;
        var spotY = y - padding;
        var spotW = w + padding * 2;
        var spotH = h + padding * 2;

        var winW = _window.Bounds.Width;
        var winH = _window.Bounds.Height;
        if (spotX < 0) { spotW += spotX; spotX = 0; }
        if (spotY < 0) { spotH += spotY; spotY = 0; }
        if (spotX + spotW > winW) spotW = Math.Max(0, winW - spotX);
        if (spotY + spotH > winH) spotH = Math.Max(0, winH - spotY);

        UpdateBackdrop(spotX, spotY, spotW, spotH);

        if (_spotlightBorder != null)
        {
            _spotlightBorder.IsVisible = true;
            Canvas.SetLeft(_spotlightBorder, spotX);
            Canvas.SetTop(_spotlightBorder, spotY);
            _spotlightBorder.Width = spotW;
            _spotlightBorder.Height = spotH;
        }

        if (_tooltipTitle != null) _tooltipTitle.Text = step.Title;
        if (_tooltipBody != null) _tooltipBody.Text = step.Body;
        if (_tooltipSkip != null) _tooltipSkip.IsVisible = false;
        if (_tooltipNext != null) _tooltipNext.IsVisible = string.IsNullOrEmpty(step.WaitForClickElement);

        PositionTooltip(step.TooltipAnchor, spotX, spotY, spotW, spotH);

        IsVisible = true;

        if (!string.IsNullOrEmpty(step.WaitForClickElement))
        {
            var clickTarget = FindControlByName(_window, step.WaitForClickElement);
            if (clickTarget != null)
                AttachWaitHandler(clickTarget, step);
        }
    }

    private void ShowWelcome(TutorialStep step)
    {
        if (_window == null) return;

        var winW = _window.Bounds.Width;
        var winH = _window.Bounds.Height;

        UpdateFullBackdrop(winW, winH);

        if (_spotlightBorder != null) _spotlightBorder.IsVisible = false;
        if (_tooltipCard != null) _tooltipCard.IsVisible = false;

        if (_welcomeTitle != null) _welcomeTitle.Text = step.Title;
        if (_welcomeBody != null) _welcomeBody.Text = step.Body;
        if (_welcomeStartBtnText != null) _welcomeStartBtnText.Text = step.CenterBtnLabel;

        if (_welcomeCard != null)
        {
            _welcomeCard.IsVisible = true;
            var cardX = (winW - WelcomeCardWidth) / 2.0;
            var cardY = (winH - WelcomeCardEstimatedHeight) / 2.0;
            Canvas.SetLeft(_welcomeCard, Math.Max(16, cardX));
            Canvas.SetTop(_welcomeCard, Math.Max(16, cardY));
        }
    }

    private void AttachWaitHandler(Control target, TutorialStep step)
    {
        _waitTarget = target;
        _waitHandler = (_, _) =>
        {
            DetachWaitHandler();
            Dispatcher.UIThread.Post(() =>
            {
                if (step.HideOverlayAfterAction)
                    TutorialService.Instance.HideForAction();
                else
                    TutorialService.Instance.Next();
            }, DispatcherPriority.Background);
        };
        _waitTarget.AddHandler(
            PointerReleasedEvent,
            _waitHandler,
            Avalonia.Interactivity.RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    private void DetachWaitHandler()
    {
        if (_waitTarget != null && _waitHandler != null)
        {
            _waitTarget.RemoveHandler(PointerReleasedEvent, _waitHandler);
            _waitTarget = null;
            _waitHandler = null;
        }
    }

    private static Control? FindControlByName(Control root, string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (root.Name == name) return root;
        foreach (var child in root.GetVisualChildren())
        {
            if (child is Control c)
            {
                var found = FindControlByName(c, name);
                if (found != null) return found;
            }
        }
        return null;
    }

    private void UpdateBackdrop(double spotX, double spotY, double spotW, double spotH)
    {
        if (_backdropPath == null || _window == null) return;

        var winW = _window.Bounds.Width;
        var winH = _window.Bounds.Height;

        var outer = new PathFigure
        {
            StartPoint = new Point(0, 0),
            IsClosed = true,
            Segments = new PathSegments
            {
                new LineSegment { Point = new Point(winW, 0) },
                new LineSegment { Point = new Point(winW, winH) },
                new LineSegment { Point = new Point(0, winH) }
            }
        };

        var inner = new PathFigure
        {
            StartPoint = new Point(spotX, spotY),
            IsClosed = true,
            Segments = new PathSegments
            {
                new LineSegment { Point = new Point(spotX + spotW, spotY) },
                new LineSegment { Point = new Point(spotX + spotW, spotY + spotH) },
                new LineSegment { Point = new Point(spotX, spotY + spotH) }
            }
        };

        _backdropPath.Data = new PathGeometry
        {
            FillRule = FillRule.EvenOdd,
            Figures = new PathFigures { outer, inner }
        };

        _backdropPath.Width = winW;
        _backdropPath.Height = winH;
    }

    private void UpdateFullBackdrop(double winW, double winH)
    {
        if (_backdropPath == null) return;

        var outer = new PathFigure
        {
            StartPoint = new Point(0, 0),
            IsClosed = true,
            Segments = new PathSegments
            {
                new LineSegment { Point = new Point(winW, 0) },
                new LineSegment { Point = new Point(winW, winH) },
                new LineSegment { Point = new Point(0, winH) }
            }
        };

        _backdropPath.Data = new PathGeometry
        {
            FillRule = FillRule.EvenOdd,
            Figures = new PathFigures { outer }
        };

        _backdropPath.Width = winW;
        _backdropPath.Height = winH;
    }

    private void PositionTooltip(TooltipAnchor anchor, double spotX, double spotY, double spotW, double spotH)
    {
        if (_tooltipCard == null || _window == null) return;

        const double gap = 12;
        var winW = _window.Bounds.Width;
        var winH = _window.Bounds.Height;
        const double margin = 8;

        double tipX, tipY;

        switch (anchor)
        {
            case TooltipAnchor.Right:
                tipX = spotX + spotW + gap;
                tipY = spotY;
                break;
            case TooltipAnchor.Left:
                tipX = spotX - TooltipMaxWidth - gap;
                tipY = spotY;
                break;
            case TooltipAnchor.Above:
                tipX = spotX;
                tipY = spotY - TooltipEstimatedHeight - gap;
                break;
            default:
                tipX = spotX;
                tipY = spotY + spotH + gap;
                break;
        }

        tipX = Math.Max(margin, Math.Min(tipX, winW - TooltipMaxWidth - margin));
        tipY = Math.Max(margin, Math.Min(tipY, winH - TooltipEstimatedHeight - margin));

        Canvas.SetLeft(_tooltipCard, tipX);
        Canvas.SetTop(_tooltipCard, tipY);
    }

    private void OnTutorialHidden()
    {
        IsVisible = false;
    }

    private void OnTutorialResumed()
    {
        IsVisible = true;
    }

    private void OnTutorialEnded()
    {
        DetachWaitHandler();
        IsVisible = false;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DetachWaitHandler();
        TutorialService.Instance.TutorialStarted -= OnTutorialStarted;
        TutorialService.Instance.StepChanged -= OnStepChanged;
        TutorialService.Instance.TutorialEnded -= OnTutorialEnded;
        TutorialService.Instance.TutorialHidden -= OnTutorialHidden;
        TutorialService.Instance.TutorialResumed -= OnTutorialResumed;
    }
}
