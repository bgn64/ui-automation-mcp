using UIAutomation.Core.Models;

namespace UIAutomation.Core.Tests;

public class ElementQueryResultTests
{
    [Fact]
    public void CanConstruct_WithRequiredProperties()
    {
        var result = new ElementQueryResult
        {
            Elements = [new ElementInfo { ElementId = "e-1" }],
            MatchedCount = 1,
            ScannedCount = 10,
            Truncated = false,
        };

        Assert.Single(result.Elements);
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(10, result.ScannedCount);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void Truncated_CanBeTrue()
    {
        var result = new ElementQueryResult
        {
            Elements = [],
            MatchedCount = 300,
            ScannedCount = 500,
            Truncated = true,
        };

        Assert.True(result.Truncated);
        Assert.Equal(300, result.MatchedCount);
    }
}
