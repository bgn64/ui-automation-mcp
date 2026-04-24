using UIAutomation.Core.Models;

namespace UIAutomation.Core.Tests;

public class ElementInfoTests
{
    [Fact]
    public void ElementInfo_CanBeConstructed_WithRequiredProperties()
    {
        var info = new ElementInfo
        {
            ElementId = "e-1",
            Name = "Test Button",
            ControlType = "Button",
            IsEnabled = true,
        };

        Assert.Equal("e-1", info.ElementId);
        Assert.Equal("Test Button", info.Name);
        Assert.Equal("Button", info.ControlType);
        Assert.True(info.IsEnabled);
    }

    [Fact]
    public void ElementInfo_DefaultValues_AreCorrect()
    {
        var info = new ElementInfo { ElementId = "e-1" };

        Assert.Equal("", info.Name);
        Assert.Equal("", info.AutomationId);
        Assert.Equal("", info.ControlType);
        Assert.Equal("", info.ClassName);
        Assert.Null(info.BoundingRectangle);
        Assert.False(info.IsEnabled);
        Assert.False(info.IsOffscreen);
        Assert.Empty(info.SupportedPatterns);
        Assert.Null(info.Children);
    }

    [Fact]
    public void ElementInfo_ToString_FormatsCorrectly()
    {
        var info = new ElementInfo
        {
            ElementId = "e-42",
            Name = "OK",
            AutomationId = "btnOk",
            ControlType = "Button",
        };

        var str = info.ToString();
        Assert.Contains("Button", str);
        Assert.Contains("OK", str);
        Assert.Contains("btnOk", str);
        Assert.Contains("e-42", str);
    }

    [Fact]
    public void BoundsInfo_CanBeConstructed()
    {
        var bounds = new BoundsInfo { X = 10, Y = 20, Width = 100, Height = 50 };

        Assert.Equal(10, bounds.X);
        Assert.Equal(20, bounds.Y);
        Assert.Equal(100, bounds.Width);
        Assert.Equal(50, bounds.Height);
    }

    [Fact]
    public void ElementInfo_Children_CanBeSet()
    {
        var parent = new ElementInfo { ElementId = "e-1", Name = "Parent" };
        var child = new ElementInfo { ElementId = "e-2", Name = "Child" };

        parent.Children = [child];

        Assert.Single(parent.Children);
        Assert.Equal("Child", parent.Children[0].Name);
    }

    [Fact]
    public void ElementInfo_SerializesToJson()
    {
        var info = new ElementInfo
        {
            ElementId = "e-1",
            Name = "Test",
            ControlType = "Button",
            SupportedPatterns = ["Invoke"],
        };

        var json = System.Text.Json.JsonSerializer.Serialize(info);

        Assert.Contains("\"ElementId\"", json);
        Assert.Contains("\"e-1\"", json);
        Assert.Contains("\"Invoke\"", json);
    }
}
