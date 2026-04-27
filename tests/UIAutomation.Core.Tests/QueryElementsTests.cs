using UIAutomation.Core;
using UIAutomation.Core.Models;
using UIAutomation.Core.Services;

namespace UIAutomation.Core.Tests;

/// <summary>
/// Integration tests for QueryElements on UIAutomationService.
/// These tests run against real UI Automation (desktop must have at least one window).
/// </summary>
[Trait("Category", "Integration")]
public class QueryElementsTests
{
    private readonly UIAutomationService _service;
    private readonly ElementCache _cache;

    public QueryElementsTests()
    {
        _cache = new ElementCache();
        _service = new UIAutomationService(_cache);
    }

    private string GetFirstWindowId()
    {
        var windows = _service.ListWindows();
        Assert.NotEmpty(windows);
        return windows[0].ElementId;
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_FlatNoFilter_ReturnsElements()
    {
        var windowId = GetFirstWindowId();

        var result = _service.QueryElements(windowId, new ElementQueryOptions
        {
            Flatten = true,
            MaxDepth = 2,
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Elements);
        Assert.True(result.ScannedCount > 0);
        Assert.True(result.MatchedCount > 0);
        Assert.False(result.Truncated);

        // Flat mode: no children on result elements
        foreach (var element in result.Elements)
        {
            Assert.Null(element.Children);
        }
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_TreeNoFilter_ReturnsTreeStructure()
    {
        var windowId = GetFirstWindowId();

        var result = _service.QueryElements(windowId, new ElementQueryOptions
        {
            Flatten = false,
            MaxDepth = 2,
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Elements);
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_FilterByControlType_OnlyReturnsMatching()
    {
        var windowId = GetFirstWindowId();

        var result = _service.QueryElements(windowId, new ElementQueryOptions
        {
            Filter = new ElementFilter { ControlTypes = ["Button"] },
            Flatten = true,
            MaxDepth = 3,
        });

        Assert.NotNull(result);
        foreach (var element in result.Elements)
        {
            Assert.Equal("Button", element.ControlType);
        }
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_FilterByEnabledState_OnlyReturnsEnabled()
    {
        var windowId = GetFirstWindowId();

        var result = _service.QueryElements(windowId, new ElementQueryOptions
        {
            Filter = new ElementFilter { IsEnabled = true },
            Flatten = true,
            MaxDepth = 2,
        });

        Assert.NotNull(result);
        foreach (var element in result.Elements)
        {
            Assert.True(element.IsEnabled);
        }
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_MaxResults_TruncatesOutput()
    {
        var windowId = GetFirstWindowId();

        var result = _service.QueryElements(windowId, new ElementQueryOptions
        {
            Flatten = true,
            MaxResults = 2,
            MaxDepth = 5,
        });

        Assert.NotNull(result);
        Assert.True(result.Elements.Count <= 2);
        // If more than 2 matches exist, should be marked truncated
        if (result.MatchedCount > 2)
        {
            Assert.True(result.Truncated);
        }
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_MaxDepth1_OnlyDirectChildren()
    {
        var windowId = GetFirstWindowId();

        var resultDepth1 = _service.QueryElements(windowId, new ElementQueryOptions
        {
            Flatten = true,
            MaxDepth = 1,
        });

        var resultDepth3 = _service.QueryElements(windowId, new ElementQueryOptions
        {
            Flatten = true,
            MaxDepth = 3,
        });

        // Depth 3 should find at least as many elements as depth 1
        Assert.True(resultDepth3.ScannedCount >= resultDepth1.ScannedCount);
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_NullOptions_UsesDefaults()
    {
        var windowId = GetFirstWindowId();

        var result = _service.QueryElements(windowId);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Elements);
        // Default is flatten = true, so no children
        foreach (var element in result.Elements)
        {
            Assert.Null(element.Children);
        }
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_OnlyCachesMatchedElements()
    {
        // Use a fresh cache to verify caching behavior
        var cache = new ElementCache();
        var service = new UIAutomationService(cache);

        var windows = service.ListWindows();
        Assert.NotEmpty(windows);
        int cacheCountAfterListWindows = cache.Count;

        var result = service.QueryElements(windows[0].ElementId, new ElementQueryOptions
        {
            Filter = new ElementFilter { ControlTypes = ["Button"] },
            Flatten = true,
            MaxDepth = 3,
        });

        // Cache should grow by exactly the number of matched elements returned
        // (plus any that were already cached from ListWindows)
        int newlyCached = cache.Count - cacheCountAfterListWindows;
        Assert.Equal(result.Elements.Count, newlyCached);
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_ThrowsForUnknownRootId()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _service.QueryElements("e-nonexistent"));
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_FilterByOffscreen_Works()
    {
        var windowId = GetFirstWindowId();

        var result = _service.QueryElements(windowId, new ElementQueryOptions
        {
            Filter = new ElementFilter { IsOffscreen = false },
            Flatten = true,
            MaxDepth = 2,
        });

        Assert.NotNull(result);
        foreach (var element in result.Elements)
        {
            Assert.False(element.IsOffscreen);
        }
    }

    [RequiresInteractiveDesktopFact]
    public void QueryElements_EmptyFilter_MatchesAll()
    {
        var windowId = GetFirstWindowId();

        var resultNoFilter = _service.QueryElements(windowId, new ElementQueryOptions
        {
            Flatten = true,
            MaxDepth = 2,
        });

        var resultEmptyFilter = _service.QueryElements(windowId, new ElementQueryOptions
        {
            Filter = new ElementFilter(),
            Flatten = true,
            MaxDepth = 2,
        });

        // An empty filter should match the same elements as no filter
        Assert.Equal(resultNoFilter.MatchedCount, resultEmptyFilter.MatchedCount);
    }
}
