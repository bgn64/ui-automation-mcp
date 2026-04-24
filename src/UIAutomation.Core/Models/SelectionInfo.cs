namespace UIAutomation.Core.Models;

/// <summary>
/// Information about the selection state of a container element (SelectionPattern).
/// </summary>
public sealed class SelectionInfo
{
    public required List<ElementInfo> SelectedItems { get; init; }
    public bool CanSelectMultiple { get; init; }
    public bool IsSelectionRequired { get; init; }
}
