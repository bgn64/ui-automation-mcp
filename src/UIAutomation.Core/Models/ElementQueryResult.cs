namespace UIAutomation.Core.Models;

/// <summary>
/// Result metadata returned alongside query results.
/// </summary>
public sealed class ElementQueryResult
{
    /// <summary>The matching elements (flat list or tree depending on query options).</summary>
    public required List<ElementInfo> Elements { get; init; }

    /// <summary>Total number of elements that matched the filter.</summary>
    public int MatchedCount { get; init; }

    /// <summary>Total number of elements scanned during the tree walk.</summary>
    public int ScannedCount { get; init; }

    /// <summary>True if results were truncated because matchedCount exceeded maxResults.</summary>
    public bool Truncated { get; init; }
}
