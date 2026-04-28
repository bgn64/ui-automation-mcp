using UIAutomation.Core.Models;

namespace UIAutomation.Core.Services;

/// <summary>
/// Platform-neutral UI automation operations exposed to the MCP tool layer.
/// All element references use string IDs managed by the active platform backend.
/// </summary>
public interface IUIAutomationService
{
    /// <summary>Lists visible top-level windows on the desktop.</summary>
    IReadOnlyList<ElementInfo> ListWindows();

    /// <summary>Finds elements within a parent element matching the given criteria.</summary>
    IReadOnlyList<ElementInfo> FindElements(string parentElementId, string? name = null, string? automationId = null, string? controlType = null);

    /// <summary>Gets detailed info about a cached element.</summary>
    ElementInfo? GetElementInfo(string elementId);

    /// <summary>Gets the UI subtree under an element, limited to maxDepth levels.</summary>
    IReadOnlyList<ElementInfo> GetElementTree(string elementId, int maxDepth = 3);

    /// <summary>
    /// Queries elements under a root element with flexible filtering, depth control,
    /// flattening, and result limiting. Only matching elements are cached.
    /// </summary>
    ElementQueryResult QueryElements(string rootElementId, ElementQueryOptions? options = null);

    /// <summary>Invokes (clicks) an element using the platform's default action.</summary>
    void InvokeElement(string elementId);

    /// <summary>Sets the value of an element.</summary>
    void SetValue(string elementId, string value);

    /// <summary>Gets the value of an element.</summary>
    string GetValue(string elementId);

    /// <summary>Toggles an element. Returns the new toggle state.</summary>
    string ToggleElement(string elementId);

    /// <summary>
    /// Simulates a physical mouse click at the element's clickable point.
    /// Uses GetClickablePoint when available, falling back to the bounding rectangle center.
    /// This is a fallback for elements that do not support InvokePattern.
    /// </summary>
    void ClickAtPoint(string elementId);

    /// <summary>Expands an element. Returns the new state.</summary>
    string ExpandElement(string elementId);

    /// <summary>Collapses an element. Returns the new state.</summary>
    string CollapseElement(string elementId);

    /// <summary>Selects an element.</summary>
    void SelectElement(string elementId);

    /// <summary>Removes an element from the selection.</summary>
    void DeselectElement(string elementId);

    /// <summary>Gets selection information from a container.</summary>
    SelectionInfo GetSelection(string elementId);

    /// <summary>Sets the visual state of a window (Minimized, Maximized, Normal).</summary>
    string SetWindowVisualState(string elementId, string state);

    /// <summary>Closes a window.</summary>
    void CloseWindow(string elementId);

    /// <summary>Gets platform-neutral state information for a window element.</summary>
    WindowStateInfo GetWindowInfo(string elementId);

    /// <summary>Scrolls a container element with relative amounts.</summary>
    ScrollInfo Scroll(string elementId, string? horizontalAmount, string? verticalAmount);

    /// <summary>Scrolls a container element to an absolute scroll position.</summary>
    ScrollInfo SetScrollPercent(string elementId, double? horizontalPercent, double? verticalPercent);

    /// <summary>Scrolls an element into view.</summary>
    void ScrollIntoView(string elementId);

    /// <summary>Gets the currently focused element.</summary>
    ElementInfo GetFocusedElement();

    /// <summary>Sets focus to an element.</summary>
    void SetFocus(string elementId);

    /// <summary>Sends keyboard input to the element after setting focus.</summary>
    void SendKeys(string elementId, string keys);

    /// <summary>Gets range value information.</summary>
    RangeValueInfo GetRangeValue(string elementId);

    /// <summary>Sets the value of a range element. Returns updated info.</summary>
    RangeValueInfo SetRangeValue(string elementId, double value);

    /// <summary>Gets text content from an element.</summary>
    string GetText(string elementId, int maxLength = -1);

    /// <summary>Gets the element at a specific row and column in a grid.</summary>
    GridInfo GetGridItem(string elementId, int row, int column);

    /// <summary>Gets table header information.</summary>
    TableHeaderInfo GetTableHeaders(string elementId);

    /// <summary>Moves an element to a new position.</summary>
    void MoveElement(string elementId, double x, double y);

    /// <summary>Resizes an element.</summary>
    void ResizeElement(string elementId, double width, double height);

    /// <summary>Gets the parent element.</summary>
    ElementInfo? GetParent(string elementId);
}
