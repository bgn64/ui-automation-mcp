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
}
