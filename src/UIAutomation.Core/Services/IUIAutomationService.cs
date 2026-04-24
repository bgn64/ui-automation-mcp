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

    /// <summary>Expands an element using ExpandCollapsePattern. Returns the new state.</summary>
    string ExpandElement(string elementId);

    /// <summary>Collapses an element using ExpandCollapsePattern. Returns the new state.</summary>
    string CollapseElement(string elementId);

    /// <summary>Selects an element using SelectionItemPattern.Select().</summary>
    void SelectElement(string elementId);

    /// <summary>Removes an element from the selection using SelectionItemPattern.RemoveFromSelection().</summary>
    void DeselectElement(string elementId);

    /// <summary>Gets selection information from a container using SelectionPattern.</summary>
    SelectionInfo GetSelection(string elementId);

    /// <summary>Sets the visual state of a window (Minimized, Maximized, Normal) using WindowPattern.</summary>
    string SetWindowVisualState(string elementId, string state);

    /// <summary>Closes a window using WindowPattern.Close().</summary>
    void CloseWindow(string elementId);

    /// <summary>Gets window information using WindowPattern.</summary>
    WindowInfo GetWindowInfo(string elementId);

    /// <summary>Scrolls a container element using ScrollPattern with relative amounts.</summary>
    ScrollInfo Scroll(string elementId, string? horizontalAmount, string? verticalAmount);

    /// <summary>Scrolls a container element using ScrollPattern to an absolute scroll position.</summary>
    ScrollInfo SetScrollPercent(string elementId, double? horizontalPercent, double? verticalPercent);

    /// <summary>Scrolls an element into view using ScrollItemPattern.</summary>
    void ScrollIntoView(string elementId);

    /// <summary>Gets the currently focused element.</summary>
    ElementInfo GetFocusedElement();

    /// <summary>Sets focus to an element.</summary>
    void SetFocus(string elementId);

    /// <summary>Sends keyboard input to the element after setting focus. Uses Win32 SendInput.</summary>
    void SendKeys(string elementId, string keys);
}
