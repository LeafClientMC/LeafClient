namespace LeafClient.Models;

public enum TooltipAnchor { Below, Above, Right, Left }

public class TutorialStep
{
    public required string TargetElementName { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public TooltipAnchor TooltipAnchor { get; init; } = TooltipAnchor.Right;
    public bool IsSkippable { get; init; } = true;
}
