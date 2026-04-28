namespace UIAutomation.Core.Models;

/// <summary>
/// Information about a range value element (sliders, spinners, progress bars) from RangeValuePattern.
/// </summary>
public sealed class RangeValueInfo
{
    public double Value { get; init; }
    public double Minimum { get; init; }
    public double Maximum { get; init; }
    public double SmallChange { get; init; }
    public double LargeChange { get; init; }
    public bool IsReadOnly { get; init; }
}
