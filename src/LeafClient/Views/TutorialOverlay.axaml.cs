using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using LeafClient.Models;
using LeafClient.Services;

namespace LeafClient.Views;

public partial class TutorialOverlay : UserControl
{
    private const double TooltipMaxWidth = 260.0;
    private const double TooltipEstimatedHeight = 120.0;
    private bool _initialized;
    private MainWindow? _window;
    private Path? _backdropPath;
    private Border? _spotlightBorder;
    private Border? _tooltipCard;
    private TextBlock? _tooltipTitle;
    private TextBlock? _tooltipBody;
    private Button? _nextBtn;
    private Button? _skipBtn;

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
        _nextBtn = this.FindControl<Button>("NextBtn");
        _skipBtn = this.FindControl<Button>("SkipBtn");

        if (_nextBtn != null) _nextBtn.Click += (_, _) => TutorialService.Instance.Next();
        if (_skipBtn != null) _skipBtn.Click += (_, _) => TutorialService.Instance.Skip();

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
        IsVisible = true;
        DetachWaitHandler();

        if (step.NavigateToPage.HasValue || step.OpenAccountPanel)
        {
            await Task.Delay(500);
        }

        if (_window == null) return;

        if (step.CenterTooltip)
        {
            ShowCentered(step);
            return;
        }

        var target = FindControlByName(_window, step.TargetElementName);
        if (target == null) return;

        var pos = target.TranslatePoint(new Point(0, 0), this);
        if (pos == null) return;

        var x = pos.Value.X;
        var y = pos.Value.Y;
        var w = target.Bounds.Width;
        var h = target.Bounds.Height;
        var padding = 6.0;

        var spotX = x - padding;
        var spotY = y - padding;
        var spotW = w + padding * 2;
        var spotH = h + padding * 2;

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
        if (_skipBtn != null) _skipBtn.IsVisible = step.IsSkippable;
        if (_nextBtn != null) _nextBtn.IsVisible = string.IsNullOrEmpty(step.WaitForClickElement);

        PositionTooltip(step.TooltipAnchor, spotX, spotY, spotW, spotH);

        if (!string.IsNullOrEmpty(step.WaitForClickElement))
        {
            var clickTarget = FindControlByName(_window, step.WaitForClickElement);
            if (clickTarget != null)
                AttachWaitHandler(clickTarget, step);
        }
    }

    private void ShowCentered(TutorialStep step)
    {
        if (_window == null) return;

        var winW = _window.Bounds.Width;
        var winH = _window.Bounds.Height;

        UpdateFullBackdrop(winW, winH);

        if (_spotlightBorder != null)
            _spotlightBorder.IsVisible = false;

        if (_tooltipTitle != null) _tooltipTitle.Text = step.Title;
        if (_tooltipBody != null) _tooltipBody.Text = step.Body;
        if (_skipBtn != null) _skipBtn.IsVisible = step.IsSkippable;
        if (_nextBtn != null) _nextBtn.IsVisible = true;

        if (_tooltipCard != null)
        {
            var tipX = (winW - TooltipMaxWidth) / 2.0;
            var tipY = (winH / 2.0) - (TooltipEstimatedHeight / 2.0);
            Canvas.SetLeft(_tooltipCard, tipX);
            Canvas.SetTop(_tooltipCard, tipY);
        }
    }

    private void AttachWaitHandler(Control target, TutorialStep step)
    {
        _waitTarget = target;
        _waitHandler = (_, _) =>
        {
            DetachWaitHandler();
            if (step.HideOverlayAfterAction)
                TutorialService.Instance.HideForAction();
            else
                TutorialService.Instance.Next();
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
