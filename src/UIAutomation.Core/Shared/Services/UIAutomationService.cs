using UIAutomation.Core.Models;
using UIAutomation.Core.Backends;

namespace UIAutomation.Core.Services;

/// <summary>
/// Shared front-end service that delegates UI automation operations to the active platform backend.
/// </summary>
public sealed class UIAutomationService : IUIAutomationService
{
    private readonly IUIAutomationBackend _backend;

    public UIAutomationService(IUIAutomationBackend backend)
    {
        _backend = backend;
    }

    public List<ElementInfo> ListWindows() => _backend.ListWindows();

    public List<ElementInfo> FindElements(string parentElementId, string? name = null, string? automationId = null, string? controlType = null) =>
        _backend.FindElements(parentElementId, name, automationId, controlType);

    public ElementInfo? GetElementInfo(string elementId) => _backend.GetElementInfo(elementId);

    public List<ElementInfo> GetElementTree(string elementId, int maxDepth = 3) => _backend.GetElementTree(elementId, maxDepth);

    public ElementQueryResult QueryElements(string rootElementId, ElementQueryOptions? options = null) =>
        _backend.QueryElements(rootElementId, options);

    public void InvokeElement(string elementId) => _backend.InvokeElement(elementId);

    public void SetValue(string elementId, string value) => _backend.SetValue(elementId, value);

    public string GetValue(string elementId) => _backend.GetValue(elementId);

    public string ToggleElement(string elementId) => _backend.ToggleElement(elementId);

    public void ClickAtPoint(string elementId) => _backend.ClickAtPoint(elementId);

    public string ExpandElement(string elementId) => _backend.ExpandElement(elementId);

    public string CollapseElement(string elementId) => _backend.CollapseElement(elementId);

    public void SelectElement(string elementId) => _backend.SelectElement(elementId);

    public void DeselectElement(string elementId) => _backend.DeselectElement(elementId);

    public SelectionInfo GetSelection(string elementId) => _backend.GetSelection(elementId);

    public string SetWindowVisualState(string elementId, string state) => _backend.SetWindowVisualState(elementId, state);

    public void CloseWindow(string elementId) => _backend.CloseWindow(elementId);

    public WindowStateInfo GetWindowInfo(string elementId) => _backend.GetWindowInfo(elementId);

    public ScrollInfo Scroll(string elementId, string? horizontalAmount, string? verticalAmount) =>
        _backend.Scroll(elementId, horizontalAmount, verticalAmount);

    public ScrollInfo SetScrollPercent(string elementId, double? horizontalPercent, double? verticalPercent) =>
        _backend.SetScrollPercent(elementId, horizontalPercent, verticalPercent);

    public void ScrollIntoView(string elementId) => _backend.ScrollIntoView(elementId);

    public ElementInfo GetFocusedElement() => _backend.GetFocusedElement();

    public void SetFocus(string elementId) => _backend.SetFocus(elementId);

    public void SendKeys(string elementId, string keys) => _backend.SendKeys(elementId, keys);

    public RangeValueInfo GetRangeValue(string elementId) => _backend.GetRangeValue(elementId);

    public RangeValueInfo SetRangeValue(string elementId, double value) => _backend.SetRangeValue(elementId, value);

    public string GetText(string elementId, int maxLength = -1) => _backend.GetText(elementId, maxLength);

    public GridInfo GetGridItem(string elementId, int row, int column) => _backend.GetGridItem(elementId, row, column);

    public TableHeaderInfo GetTableHeaders(string elementId) => _backend.GetTableHeaders(elementId);

    public void MoveElement(string elementId, double x, double y) => _backend.MoveElement(elementId, x, y);

    public void ResizeElement(string elementId, double width, double height) => _backend.ResizeElement(elementId, width, height);

    public ElementInfo? GetParent(string elementId) => _backend.GetParent(elementId);
}
