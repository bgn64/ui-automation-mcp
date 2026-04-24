using UIAutomation.Core.Models;

namespace UIAutomation.Core.Tests;

public class ElementQueryOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new ElementQueryOptions();

        Assert.Null(options.Filter);
        Assert.Null(options.MaxDepth);
        Assert.True(options.Flatten);
        Assert.Equal(200, options.MaxResults);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var filter = new ElementFilter { ControlTypes = ["Button"] };
        var options = new ElementQueryOptions
        {
            Filter = filter,
            MaxDepth = 5,
            Flatten = false,
            MaxResults = 50,
        };

        Assert.Same(filter, options.Filter);
        Assert.Equal(5, options.MaxDepth);
        Assert.False(options.Flatten);
        Assert.Equal(50, options.MaxResults);
    }
}
