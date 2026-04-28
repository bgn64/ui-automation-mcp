namespace UIAutomation.Core.Models;

/// <summary>
/// Result of a grid item lookup from GridPattern, including the element and grid dimensions.
/// </summary>
public sealed class GridInfo
{
    public required ElementInfo Item { get; init; }
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
}
