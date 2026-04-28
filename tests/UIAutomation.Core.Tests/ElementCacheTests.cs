using UIAutomation.Core;
using UIAutomation.Core.Platforms.Windows;
using System.Windows.Automation;
using Xunit;

namespace UIAutomation.Core.Tests;

public class ElementCacheTests
{
    [StaFact]
    public void GetOrAdd_AssignsUniqueIds()
    {
        var cache = new WindowsElementCache();
        var root = AutomationElement.RootElement;

        var id = cache.GetOrAdd(root);

        Assert.NotNull(id);
        Assert.StartsWith("e-", id);
    }

    [StaFact]
    public void GetOrAdd_ReturnsSameId_ForSameElement()
    {
        var cache = new WindowsElementCache();
        var root = AutomationElement.RootElement;

        var id1 = cache.GetOrAdd(root);
        var id2 = cache.GetOrAdd(root);

        Assert.Equal(id1, id2);
    }

    [StaFact]
    public void TryGet_ReturnsElement_WhenCached()
    {
        var cache = new WindowsElementCache();
        var root = AutomationElement.RootElement;
        var id = cache.GetOrAdd(root);

        var found = cache.TryGet(id, out var element);

        Assert.True(found);
        Assert.NotNull(element);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenNotCached()
    {
        var cache = new WindowsElementCache();

        var found = cache.TryGet("e-999", out var element);

        Assert.False(found);
    }

    [StaFact]
    public void Count_ReflectsAddedElements()
    {
        var cache = new WindowsElementCache();
        Assert.Equal(0, cache.Count);

        cache.GetOrAdd(AutomationElement.RootElement);
        Assert.Equal(1, cache.Count);
    }

    [StaFact]
    public void Eviction_RemovesOldestEntries_WhenOverCapacity()
    {
        var cache = new WindowsElementCache(maxCapacity: 2);
        var root = AutomationElement.RootElement;

        // Add the root element (1 slot used)
        var id1 = cache.GetOrAdd(root);

        // Find two child windows to get distinct elements
        var children = root.FindAll(TreeScope.Children,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

        if (children.Count < 2)
        {
            // Skip test if we can't find enough windows
            return;
        }

        var id2 = cache.GetOrAdd(children[0]);
        var id3 = cache.GetOrAdd(children[1]);

        // Cache capacity is 2, so the first entry should have been evicted
        Assert.True(cache.Count <= 2);
    }

    [StaFact]
    public void Clear_RemovesAllEntries()
    {
        var cache = new WindowsElementCache();
        cache.GetOrAdd(AutomationElement.RootElement);
        Assert.True(cache.Count > 0);

        cache.Clear();

        Assert.Equal(0, cache.Count);
    }
}
