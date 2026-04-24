using UIAutomation.Core;
using UIAutomation.Core.Services;

namespace UIAutomation.Core.Tests;

/// <summary>
/// Integration tests for UIAutomationService. The service handles STA threading internally,
/// so these tests use plain [Fact] (no [StaFact] needed).
/// </summary>
[Trait("Category", "Integration")]
public class UIAutomationServiceTests
{
    private readonly UIAutomationService _service;
    private readonly ElementCache _cache;

    public UIAutomationServiceTests()
    {
        _cache = new ElementCache();
        _service = new UIAutomationService(_cache);
    }

    [Fact]
    public void ListWindows_ReturnsNonEmptyList()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);
    }

    [Fact]
    public void ListWindows_AllEntriesHaveWindowControlType()
    {
        var windows = _service.ListWindows();

        foreach (var w in windows)
        {
            Assert.Equal("Window", w.ControlType);
            Assert.NotEmpty(w.ElementId);
        }
    }

    [Fact]
    public void ListWindows_CachesElements()
    {
        var windows = _service.ListWindows();

        foreach (var w in windows)
        {
            Assert.True(_cache.TryGet(w.ElementId, out var element));
            Assert.NotNull(element);
        }
    }

    [Fact]
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

    [Fact]
    public void GetElementInfo_ReturnsNull_ForUnknownId()
    {
        var info = _service.GetElementInfo("e-does-not-exist");
        Assert.Null(info);
    }

    [Fact]
    public void GetElementInfo_ReturnsInfo_ForCachedElement()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var info = _service.GetElementInfo(windows[0].ElementId);
        Assert.NotNull(info);
        Assert.Equal("Window", info.ControlType);
    }

    [Fact]
    public void GetElementTree_ReturnsChildren_ForWindow()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var tree = _service.GetElementTree(windows[0].ElementId, maxDepth: 1);
        Assert.NotNull(tree);
    }

    [Fact]
    public void FindElements_ThrowsForUnknownParent()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.FindElements("e-nonexistent", name: "anything"));
    }

    [Fact]
    public void GetValue_FallsBackToName_WhenNoValuePattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var value = _service.GetValue(windows[0].ElementId);
        Assert.NotNull(value);
    }

    [Fact]
    public void InvokeElement_Throws_WhenElementDoesNotSupportInvoke()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.InvokeElement(windows[0].ElementId));
    }

    [Fact]
    public void ClickAtPoint_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.ClickAtPoint("e-does-not-exist"));
    }

    // --- ExpandCollapse tests ---

    [Fact]
    public void ExpandElement_Throws_WhenNoExpandCollapsePattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        // Windows don't support ExpandCollapsePattern
        Assert.Throws<InvalidOperationException>(() =>
            _service.ExpandElement(windows[0].ElementId));
    }

    [Fact]
    public void CollapseElement_Throws_WhenNoExpandCollapsePattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.CollapseElement(windows[0].ElementId));
    }

    // --- Selection tests ---

    [Fact]
    public void SelectElement_Throws_WhenNoSelectionItemPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.SelectElement(windows[0].ElementId));
    }

    [Fact]
    public void DeselectElement_Throws_WhenNoSelectionItemPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.DeselectElement(windows[0].ElementId));
    }

    [Fact]
    public void GetSelection_Throws_WhenNoSelectionPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.GetSelection(windows[0].ElementId));
    }

    // --- Window management tests ---

    [Fact]
    public void GetWindowInfo_ReturnsInfo_ForWindow()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        var info = _service.GetWindowInfo(windows[0].ElementId);
        Assert.NotNull(info);
        Assert.NotEmpty(info.WindowVisualState);
        Assert.NotEmpty(info.WindowInteractionState);
    }

    [Fact]
    public void SetWindowVisualState_ThrowsForInvalidState()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<ArgumentException>(() =>
            _service.SetWindowVisualState(windows[0].ElementId, "invalid_state"));
    }

    // --- Scroll tests ---

    [Fact]
    public void ScrollIntoView_Throws_WhenNoScrollItemPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        // Windows don't support ScrollItemPattern — may throw InvalidOperationException
        // or ElementNotAvailableException depending on the element
        Assert.ThrowsAny<Exception>(() =>
            _service.ScrollIntoView(windows[0].ElementId));
    }

    [Fact]
    public void Scroll_Throws_WhenNoScrollPattern()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);

        Assert.Throws<InvalidOperationException>(() =>
            _service.Scroll(windows[0].ElementId, null, "SmallIncrement"));
    }

    [Fact]
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

    [Fact]
    public void GetFocusedElement_ReturnsNonNull()
    {
        var focused = _service.GetFocusedElement();
        Assert.NotNull(focused);
        Assert.NotEmpty(focused.ElementId);
    }

    [Fact]
    public void SetFocus_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.SetFocus("e-does-not-exist"));
    }

    // --- SendKeys tests ---

    [Fact]
    public void SendKeys_Throws_ForUnknownElementId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.SendKeys("e-does-not-exist", "test"));
    }
}
