namespace LeafClient.Models;

public enum TooltipAnchor { Below, Above, Right, Left }

public enum TutorialOnEnter { None, SelectLeafCapeInStore }

public class TutorialStep
{
    public required string TargetElementName { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public TooltipAnchor TooltipAnchor { get; init; } = TooltipAnchor.Right;
    public bool IsSkippable { get; init; } = true;
    public int? NavigateToPage { get; init; }
    public string? WaitForClickElement { get; init; }
    public bool OpenAccountPanel { get; init; }
    public bool HideOverlayAfterAction { get; init; }
    public TutorialOnEnter OnEnter { get; init; } = TutorialOnEnter.None;
    public bool CenterTooltip { get; init; }
    public string SkipLabel { get; init; } = "Skip →";
    public string CenterBtnLabel { get; init; } = "Get Started →";
    public bool SkipIfCracked { get; init; } = false;
}
