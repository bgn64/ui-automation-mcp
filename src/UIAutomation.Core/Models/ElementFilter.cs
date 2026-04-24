namespace UIAutomation.Core.Models;

/// <summary>
/// Criteria for filtering UI elements during a query.
/// All non-null criteria are AND'd together.
/// Within array criteria (ControlTypes, SupportedPatterns), values are OR'd.
/// String matching is case-insensitive (OrdinalIgnoreCase).
/// </summary>
public sealed class ElementFilter
{
    /// <summary>
    /// Include elements whose ControlType matches any of these values.
    /// Example: ["Button", "Edit", "CheckBox"]
    /// </summary>
    public string[]? ControlTypes { get; init; }

    /// <summary>
    /// Include elements that support at least one of these automation patterns.
    /// Example: ["Invoke", "Toggle", "Value"]
    /// </summary>
    public string[]? SupportedPatterns { get; init; }

    /// <summary>
    /// Include elements whose Name contains this substring (case-insensitive).
    /// </summary>
    public string? NameContains { get; init; }

    /// <summary>
    /// Include elements whose AutomationId contains this substring (case-insensitive).
    /// </summary>
    public string? AutomationIdContains { get; init; }

    /// <summary>
    /// Include elements whose ClassName contains this substring (case-insensitive).
    /// </summary>
    public string? ClassNameContains { get; init; }

    /// <summary>Filter by enabled state. Null means no filtering on this property.</summary>
    public bool? IsEnabled { get; init; }

    /// <summary>Filter by offscreen state. Null means no filtering on this property.</summary>
    public bool? IsOffscreen { get; init; }

    /// <summary>
    /// Returns true if this filter has no criteria set (matches everything).
    /// </summary>
    public bool IsEmpty =>
        (ControlTypes is null or { Length: 0 })
        && (SupportedPatterns is null or { Length: 0 })
        && string.IsNullOrEmpty(NameContains)
        && string.IsNullOrEmpty(AutomationIdContains)
        && string.IsNullOrEmpty(ClassNameContains)
        && IsEnabled is null
        && IsOffscreen is null;
}
