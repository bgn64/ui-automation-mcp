using UIAutomation.Core.Models;

namespace UIAutomation.Core.Services;

/// <summary>
/// Abstraction over Windows UI Automation operations.
/// All element references use string IDs managed by an ElementCache.
/// </summary>
public interface IUIAutomationService
{
    /// <summary>Lists visible top-level windows on the desktop.</summary>
    List<ElementInfo> ListWindows();

    /// <summary>Finds elements within a parent element matching the given criteria.</summary>
    List<ElementInfo> FindElements(string parentElementId, string? name = null, string? automationId = null, string? controlType = null);

    /// <summary>Gets detailed info about a cached element.</summary>
    ElementInfo? GetElementInfo(string elementId);

    /// <summary>Gets the UI subtree under an element, limited to maxDepth levels.</summary>
    List<ElementInfo> GetElementTree(string elementId, int maxDepth = 3);

    /// <summary>
    /// Queries elements under a root element with flexible filtering, depth control,
    /// flattening, and result limiting. Only matching elements are cached.
    /// </summary>
    ElementQueryResult QueryElements(string rootElementId, ElementQueryOptions? options = null);

    /// <summary>Invokes (clicks) an element using InvokePattern.</summary>
    void InvokeElement(string elementId);

    /// <summary>Sets the value of an element using ValuePattern.</summary>
    void SetValue(string elementId, string value);

    /// <summary>Gets the value of an element (ValuePattern or Name property).</summary>
    string GetValue(string elementId);

    /// <summary>Toggles an element using TogglePattern. Returns the new toggle state.</summary>
    string ToggleElement(string elementId);

    /// <summary>
    /// Simulates a physical mouse click at the element's clickable point.
    /// Uses GetClickablePoint when available, falling back to the bounding rectangle center.
    /// This is a fallback for elements that do not support InvokePattern.
    /// </summary>
    void ClickAtPoint(string elementId);
}
