using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using LeafClient.Models;
using LeafClient.Services;

namespace LeafClient.Views;

public partial class TutorialOverlay : UserControl
{
    private MainWindow? _window;
    private Path? _backdropPath;
    private Border? _spotlightBorder;
    private Border? _tooltipCard;
    private TextBlock? _tooltipTitle;
    private TextBlock? _tooltipBody;
    private Button? _nextBtn;
    private Button? _skipBtn;

    public TutorialOverlay()
    {
        InitializeComponent();
    }

    public void Initialize(MainWindow window)
    {
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
    }

    private void OnTutorialStarted()
    {
        IsVisible = true;
    }

    private void OnStepChanged(TutorialStep step)
    {
        if (_window == null) return;

        var target = _window.FindControl<Control>(step.TargetElementName);
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
            Canvas.SetLeft(_spotlightBorder, spotX);
            Canvas.SetTop(_spotlightBorder, spotY);
            _spotlightBorder.Width = spotW;
            _spotlightBorder.Height = spotH;
        }

        if (_tooltipTitle != null) _tooltipTitle.Text = step.Title;
        if (_tooltipBody != null) _tooltipBody.Text = step.Body;
        if (_skipBtn != null) _skipBtn.IsVisible = step.IsSkippable;

        PositionTooltip(step.TooltipAnchor, spotX, spotY, spotW, spotH);
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

    private void PositionTooltip(TooltipAnchor anchor, double spotX, double spotY, double spotW, double spotH)
    {
        if (_tooltipCard == null) return;

        const double gap = 12;
        double tipX, tipY;

        switch (anchor)
        {
            case TooltipAnchor.Right:
                tipX = spotX + spotW + gap;
                tipY = spotY;
                break;
            case TooltipAnchor.Left:
                tipX = spotX - 260 - gap;
                tipY = spotY;
                break;
            case TooltipAnchor.Above:
                tipX = spotX;
                tipY = spotY - _tooltipCard.Bounds.Height - gap;
                break;
            default:
                tipX = spotX;
                tipY = spotY + spotH + gap;
                break;
        }

        Canvas.SetLeft(_tooltipCard, tipX);
        Canvas.SetTop(_tooltipCard, tipY);
    }

    private void OnTutorialEnded()
    {
        IsVisible = false;
    }
}
