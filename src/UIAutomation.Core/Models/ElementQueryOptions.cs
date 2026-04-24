namespace UIAutomation.Core.Models;

/// <summary>
/// Options for the QueryElements operation controlling depth, filtering, flattening, and result limits.
/// </summary>
public sealed class ElementQueryOptions
{
    /// <summary>
    /// Filter criteria. Null or empty filter means match all elements.
    /// </summary>
    public ElementFilter? Filter { get; init; }

    /// <summary>
    /// Maximum depth to walk into the element tree.
    /// Null means unlimited depth. 1 means only direct children, 2 means children and grandchildren, etc.
    /// </summary>
    public int? MaxDepth { get; init; }

    /// <summary>
    /// When true (default), returns a flat list of all matching elements regardless of tree position.
    /// When false, returns a tree structure preserving parent-child relationships.
    /// In tree mode, non-matching ancestor elements are included if they have matching descendants.
    /// </summary>
    public bool Flatten { get; init; } = true;

    /// <summary>
    /// Maximum number of matching elements to return. Prevents overwhelming output.
    /// Default is 200. The walk stops early once this limit is reached.
    /// </summary>
    public int MaxResults { get; init; } = 200;
}
