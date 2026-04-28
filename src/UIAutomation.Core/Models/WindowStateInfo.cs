namespace UIAutomation.Core.Models;

/// <summary>
/// Platform-neutral information about an application window's current state.
/// </summary>
public sealed class WindowStateInfo
{
    public required string WindowVisualState { get; init; }
    public required string WindowInteractionState { get; init; }
    public bool CanMaximize { get; init; }
    public bool CanMinimize { get; init; }
    public bool IsModal { get; init; }
    public bool IsTopmost { get; init; }
}
