namespace UIAutomation.Core.Models;

/// <summary>
/// Information about a window's state from WindowPattern.
/// </summary>
public sealed class WindowInfo
{
    public required string WindowVisualState { get; init; }
    public required string WindowInteractionState { get; init; }
    public bool CanMaximize { get; init; }
    public bool CanMinimize { get; init; }
    public bool IsModal { get; init; }
    public bool IsTopmost { get; init; }
}
