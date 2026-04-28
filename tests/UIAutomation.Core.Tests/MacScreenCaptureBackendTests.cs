#if MACOS
using UIAutomation.Core.Platforms.MacOS;

namespace UIAutomation.Core.Tests;

[Trait("Category", "Integration")]
public class MacScreenCaptureBackendTests
{
    [Fact]
    public void GetMonitors_ReturnsAtLeastOneMonitor()
    {
        var service = new MacScreenCaptureBackend();

        var monitors = service.GetMonitors();

        Assert.NotEmpty(monitors);
    }

    [Fact]
    public void GetMonitors_AllMonitorsHavePositiveDimensions()
    {
        var service = new MacScreenCaptureBackend();

        var monitors = service.GetMonitors();

        foreach (var monitor in monitors)
        {
            Assert.True(monitor.Width > 0, $"Monitor {monitor.Index} ({monitor.DeviceName}) has non-positive width: {monitor.Width}");
            Assert.True(monitor.Height > 0, $"Monitor {monitor.Index} ({monitor.DeviceName}) has non-positive height: {monitor.Height}");
        }
    }
}
#endif
