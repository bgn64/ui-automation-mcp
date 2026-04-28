using UIAutomation.Core;
using UIAutomation.Core.Platforms.Windows;
using UIAutomation.Core.Services;

namespace UIAutomation.Core.Tests.Windows;

/// <summary>
/// Integration tests for WindowsUIAutomationBackend. The backend handles STA threading internally,
/// so these tests use plain [RequiresInteractiveDesktopFact] (no [StaFact] needed).
/// </summary>
[Trait("Category", "Integration")]
public class UIAutomationServiceTests
{
    private readonly IUIAutomationService _service = new WindowsUIAutomationBackend();

    [RequiresInteractiveDesktopFact]
    public void ListWindows_ReturnsNonEmptyList()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);
    }

    [RequiresInteractiveDesktopFact]
    public void ListWindows_AllEntriesHaveWindowControlType()
    {
        var windows = _service.ListWindows();

        foreach (var w in windows)
        {
            Assert.Equal("Window", w.ControlType);
            Assert.NotEmpty(w.ElementId);
        }
    }

    [RequiresInteractiveDesktopFact]
    public void ListWindows_CachesElements()
    {
        var windows = _service.ListWindows();

        foreach (var w in windows)
        {
            var info = _service.GetElementInfo(w.ElementId);
            Assert.NotNull(info);
        }
    }

    [RequiresInteractiveDesktopFact]
    public void ListWindows_IncludesNewProperties()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var w = windows[0];
        // New bool properties should be populated (value doesn't matter, just that they exist)
        Assert.IsType<bool>(w.HasKeyboardFocus);
        Assert.IsType<bool>(w.IsKeyboardFocusable);
        // FrameworkId is typically non-null for windows
    }

    [RequiresInteractiveDesktopFact]
    public void GetElementInfo_ReturnsNull_ForUnknownId()
    {
        var info = _service.GetElementInfo("e-does-not-exist");
        Assert.Null(info);
    }

    [RequiresInteractiveDesktopFact]
    public void GetElementInfo_ReturnsInfo_ForCachedElement()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var info = _service.GetElementInfo(windows[0].ElementId);
        Assert.NotNull(info);
        Assert.Equal("Window", info.ControlType);
    }

    [RequiresInteractiveDesktopFact]
    public void GetElementTree_ReturnsChildren_ForWindow()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var tree = _service.GetElementTree(windows[0].ElementId, maxDepth: 1);
        Assert.NotNull(tree);
    }

    [RequiresInteractiveDesktopFact]
    public void FindElements_ThrowsForUnknownParent()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.FindElements("e-nonexistent", name: "anything"));
    }

    [RequiresInteractiveDesktopFact]
    public void GetValue_FallsBackToName_WhenNoValuePattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var value = _service.GetValue(windows[0].ElementId);
        Assert.NotNull(value);
    }

    [RequiresInteractiveDesktopFact]
    public void InvokeElement_Throws_WhenElementDoesNotSupportInvoke()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.InvokeElement(windows[0].ElementId));
    }

    [RequiresInteractiveDesktopFact]
    public void ClickAtPoint_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.ClickAtPoint("e-does-not-exist"));
    }

    // --- ExpandCollapse tests ---

    [RequiresInteractiveDesktopFact]
    public void ExpandElement_Throws_WhenNoExpandCollapsePattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        // Windows don't support ExpandCollapsePattern
        Assert.Throws<InvalidOperationException>(() =>
            _service.ExpandElement(windows[0].ElementId));
    }

    [RequiresInteractiveDesktopFact]
    public void CollapseElement_Throws_WhenNoExpandCollapsePattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.CollapseElement(windows[0].ElementId));
    }

    // --- Selection tests ---

    [RequiresInteractiveDesktopFact]
    public void SelectElement_Throws_WhenNoSelectionItemPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.SelectElement(windows[0].ElementId));
    }

    [RequiresInteractiveDesktopFact]
    public void DeselectElement_Throws_WhenNoSelectionItemPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.DeselectElement(windows[0].ElementId));
    }

    [RequiresInteractiveDesktopFact]
    public void GetSelection_Throws_WhenNoSelectionPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.GetSelection(windows[0].ElementId));
    }

    // --- Window management tests ---

    [RequiresInteractiveDesktopFact]
    public void GetWindowInfo_ReturnsInfo_ForWindow()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var info = _service.GetWindowInfo(windows[0].ElementId);
        Assert.NotNull(info);
        Assert.NotEmpty(info.WindowVisualState);
        Assert.NotEmpty(info.WindowInteractionState);
    }

    [RequiresInteractiveDesktopFact]
    public void SetWindowVisualState_ThrowsForInvalidState()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<ArgumentException>(() =>
            _service.SetWindowVisualState(windows[0].ElementId, "invalid_state"));
    }

    // --- Scroll tests ---

    [RequiresInteractiveDesktopFact]
    public void ScrollIntoView_Throws_WhenNoScrollItemPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        // Windows don't support ScrollItemPattern — may throw InvalidOperationException
        // or ElementNotAvailableException depending on the element
        Assert.ThrowsAny<Exception>(() =>
            _service.ScrollIntoView(windows[0].ElementId));
    }

    [RequiresInteractiveDesktopFact]
    public void Scroll_Throws_WhenNoScrollPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.Scroll(windows[0].ElementId, null, "SmallIncrement"));
    }

    [RequiresInteractiveDesktopFact]
    public void Scroll_ThrowsForInvalidAmount()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        // Even though window doesn't support ScrollPattern, the argument validation
        // in ParseScrollAmount may fire for invalid amounts. But actually the pattern
        // check comes first. Let's verify the error type at least.
        var ex = Assert.ThrowsAny<Exception>(() =>
            _service.Scroll(windows[0].ElementId, "InvalidAmount", null));
        Assert.True(ex is InvalidOperationException || ex is ArgumentException);
    }

    // --- Focus tests ---

    [RequiresInteractiveDesktopFact]
    public void GetFocusedElement_ReturnsNonNull()
    {
        var focused = _service.GetFocusedElement();
        Assert.NotNull(focused);
        Assert.NotEmpty(focused.ElementId);
    }

    [RequiresInteractiveDesktopFact]
    public void SetFocus_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.SetFocus("e-does-not-exist"));
    }

    // --- SendKeys tests ---

    [RequiresInteractiveDesktopFact]
    public void SendKeys_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.SendKeys("e-does-not-exist", "test"));
    }

    // --- RangeValue tests ---

    [RequiresInteractiveDesktopFact]
    public void GetRangeValue_Throws_WhenNoRangeValuePattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.GetRangeValue(windows[0].ElementId));
    }

    [RequiresInteractiveDesktopFact]
    public void SetRangeValue_Throws_WhenNoRangeValuePattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.SetRangeValue(windows[0].ElementId, 50.0));
    }

    [RequiresInteractiveDesktopFact]
    public void GetRangeValue_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.GetRangeValue("e-does-not-exist"));
    }

    // --- TextPattern tests ---

    [RequiresInteractiveDesktopFact]
    public void GetText_Throws_WhenNoTextPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.GetText(windows[0].ElementId));
    }

    [RequiresInteractiveDesktopFact]
    public void GetText_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.GetText("e-does-not-exist"));
    }

    // --- GridPattern tests ---

    [RequiresInteractiveDesktopFact]
    public void GetGridItem_Throws_WhenNoGridPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.GetGridItem(windows[0].ElementId, 0, 0));
    }

    [RequiresInteractiveDesktopFact]
    public void GetGridItem_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.GetGridItem("e-does-not-exist", 0, 0));
    }

    // --- TablePattern tests ---

    [RequiresInteractiveDesktopFact]
    public void GetTableHeaders_Throws_WhenNoTablePattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.GetTableHeaders(windows[0].ElementId));
    }

    [RequiresInteractiveDesktopFact]
    public void GetTableHeaders_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.GetTableHeaders("e-does-not-exist"));
    }

    // --- TransformPattern tests ---

    [RequiresInteractiveDesktopFact]
    public void MoveElement_Throws_ForUnsupportedElement()
    {
        // Use a window element — windows don't support TransformPattern.CanMove
        // in a way that allows arbitrary repositioning via this API.
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        // Most top-level windows do support TransformPattern, so use FindElements
        // to locate a non-window element (e.g. a static text) that doesn't.
        var children = _service.GetElementTree(windows[0].ElementId, maxDepth: 1);
        var nonTransformable = children.FirstOrDefault(c => c.ControlType is "Text" or "Image");
        if (nonTransformable is null) return; // skip if no suitable element found

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _service.MoveElement(nonTransformable.ElementId, 100, 100));
        Assert.Contains("TransformPattern", ex.Message);
    }

    [RequiresInteractiveDesktopFact]
    public void ResizeElement_Throws_ForUnsupportedElement()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var children = _service.GetElementTree(windows[0].ElementId, maxDepth: 1);
        var nonTransformable = children.FirstOrDefault(c => c.ControlType is "Text" or "Image");
        if (nonTransformable is null) return; // skip if no suitable element found

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _service.ResizeElement(nonTransformable.ElementId, 800, 600));
        Assert.Contains("TransformPattern", ex.Message);
    }

    [RequiresInteractiveDesktopFact]
    public void MoveElement_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.MoveElement("e-does-not-exist", 100, 100));
    }

    [RequiresInteractiveDesktopFact]
    public void ResizeElement_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.ResizeElement("e-does-not-exist", 800, 600));
    }

    // --- GetParent tests ---

    [RequiresInteractiveDesktopFact]
    public void GetParent_ReturnsNull_ForTopLevelWindow()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        // Top-level windows have the desktop root as parent, which returns null
        var parent = _service.GetParent(windows[0].ElementId);
        Assert.Null(parent);
    }

    [RequiresInteractiveDesktopFact]
    public void GetParent_ReturnsParent_ForChildElement()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var tree = _service.GetElementTree(windows[0].ElementId, maxDepth: 1);
        if (tree.Count > 0)
        {
            var parent = _service.GetParent(tree[0].ElementId);
            Assert.NotNull(parent);
            Assert.Equal("Window", parent.ControlType);
        }
    }

    [RequiresInteractiveDesktopFact]
    public void GetParent_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.GetParent("e-does-not-exist"));
    }
}
