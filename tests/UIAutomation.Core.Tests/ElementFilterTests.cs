using UIAutomation.Core.Models;

namespace UIAutomation.Core.Tests;

public class ElementFilterTests
{
    [Fact]
    public void IsEmpty_ReturnsTrue_WhenNoFiltersSet()
    {
        var filter = new ElementFilter();
        Assert.True(filter.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ReturnsTrue_WhenArraysAreEmpty()
    {
        var filter = new ElementFilter
        {
            ControlTypes = [],
            SupportedPatterns = [],
        };
        Assert.True(filter.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ReturnsFalse_WhenControlTypesSet()
    {
        var filter = new ElementFilter { ControlTypes = ["Button"] };
        Assert.False(filter.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ReturnsFalse_WhenSupportedPatternsSet()
    {
        var filter = new ElementFilter { SupportedPatterns = ["Invoke"] };
        Assert.False(filter.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ReturnsFalse_WhenNameContainsSet()
    {
        var filter = new ElementFilter { NameContains = "OK" };
        Assert.False(filter.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ReturnsFalse_WhenAutomationIdContainsSet()
    {
        var filter = new ElementFilter { AutomationIdContains = "btn" };
        Assert.False(filter.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ReturnsFalse_WhenClassNameContainsSet()
    {
        var filter = new ElementFilter { ClassNameContains = "WPF" };
        Assert.False(filter.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ReturnsFalse_WhenIsEnabledSet()
    {
        var filter = new ElementFilter { IsEnabled = true };
        Assert.False(filter.IsEmpty);
    }

    [Fact]
    public void IsEmpty_ReturnsFalse_WhenIsOffscreenSet()
    {
        var filter = new ElementFilter { IsOffscreen = false };
        Assert.False(filter.IsEmpty);
    }
}
