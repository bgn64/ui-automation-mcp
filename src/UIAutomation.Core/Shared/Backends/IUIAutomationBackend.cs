using UIAutomation.Core.Models;

namespace UIAutomation.Core.Backends;

/// <summary>
/// Platform-specific UI automation contract used by the shared front-end service.
/// </summary>
public interface IUIAutomationBackend
{
    List<ElementInfo> ListWindows();
    List<ElementInfo> FindElements(string parentElementId, string? name = null, string? automationId = null, string? controlType = null);
    ElementInfo? GetElementInfo(string elementId);
    List<ElementInfo> GetElementTree(string elementId, int maxDepth = 3);
    ElementQueryResult QueryElements(string rootElementId, ElementQueryOptions? options = null);
    void InvokeElement(string elementId);
    void SetValue(string elementId, string value);
    string GetValue(string elementId);
    string ToggleElement(string elementId);
    void ClickAtPoint(string elementId);
    string ExpandElement(string elementId);
    string CollapseElement(string elementId);
    void SelectElement(string elementId);
    void DeselectElement(string elementId);
    SelectionInfo GetSelection(string elementId);
    string SetWindowVisualState(string elementId, string state);
    void CloseWindow(string elementId);
    WindowStateInfo GetWindowInfo(string elementId);
    ScrollInfo Scroll(string elementId, string? horizontalAmount, string? verticalAmount);
    ScrollInfo SetScrollPercent(string elementId, double? horizontalPercent, double? verticalPercent);
    void ScrollIntoView(string elementId);
    ElementInfo GetFocusedElement();
    void SetFocus(string elementId);
    void SendKeys(string elementId, string keys);
    RangeValueInfo GetRangeValue(string elementId);
    RangeValueInfo SetRangeValue(string elementId, double value);
    string GetText(string elementId, int maxLength = -1);
    GridInfo GetGridItem(string elementId, int row, int column);
    TableHeaderInfo GetTableHeaders(string elementId);
    void MoveElement(string elementId, double x, double y);
    void ResizeElement(string elementId, double width, double height);
    ElementInfo? GetParent(string elementId);
}
