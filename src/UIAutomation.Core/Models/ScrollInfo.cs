namespace UIAutomation.Core.Models;

/// <summary>
/// Current scroll position information from ScrollPattern.
/// </summary>
public sealed class ScrollInfo
{
    public double HorizontalScrollPercent { get; init; }
    public double VerticalScrollPercent { get; init; }
    public double HorizontalViewSize { get; init; }
    public double VerticalViewSize { get; init; }
    public bool HorizontallyScrollable { get; init; }
    public bool VerticallyScrollable { get; init; }
}
