namespace UIAutomation.Core.Models;

/// <summary>
/// Table header information from TablePattern.
/// </summary>
public sealed class TableHeaderInfo
{
    public required List<ElementInfo> RowHeaders { get; init; }
    public required List<ElementInfo> ColumnHeaders { get; init; }
    public required string RowOrColumnMajor { get; init; }
}
