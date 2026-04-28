using UIAutomation.Core.Platforms.Windows;
using UIAutomation.Core.Services;

namespace UIAutomation.Core.Tests;

/// <summary>
/// Integration tests for ScreenCaptureService.
/// These run on the live desktop, so they are marked as Integration tests.
/// </summary>
[Trait("Category", "Integration")]
public class ScreenCaptureServiceTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly ScreenCaptureService _service = new(new WindowsScreenCaptureBackend());

    [RequiresInteractiveDesktopFact]
    public void CaptureScreen_ReturnsNonEmptyBytes()
    {
        byte[] result = _service.CaptureScreen();
        Assert.NotEmpty(result);
    }

    [RequiresInteractiveDesktopFact]
    public void CaptureScreen_ReturnedBytesHaveValidPngHeader()
    {
        byte[] result = _service.CaptureScreen();
        Assert.True(result.Length >= PngSignature.Length, "Result is too short to contain a PNG header.");
        Assert.Equal(PngSignature, result[..PngSignature.Length]);
    }

    [RequiresInteractiveDesktopFact]
    public void GetMonitors_ReturnsAtLeastOneMonitor()
    {
        var monitors = _service.GetMonitors();
        Assert.NotEmpty(monitors);
    }

    [RequiresInteractiveDesktopFact]
    public void GetMonitors_AllMonitorsHavePositiveDimensions()
    {
        var monitors = _service.GetMonitors();
        foreach (var monitor in monitors)
        {
            Assert.True(monitor.Width > 0, $"Monitor {monitor.Index} ({monitor.DeviceName}) has non-positive width: {monitor.Width}");
            Assert.True(monitor.Height > 0, $"Monitor {monitor.Index} ({monitor.DeviceName}) has non-positive height: {monitor.Height}");
        }
    }

    [RequiresInteractiveDesktopFact]
    public void GetMonitors_ExactlyOnePrimaryMonitor()
    {
        var monitors = _service.GetMonitors();
        int primaryCount = monitors.Count(m => m.IsPrimary);
        Assert.Equal(1, primaryCount);
    }

    [RequiresInteractiveDesktopFact]
    public void GetMonitors_IndicesAreSequentialFromZero()
    {
        var monitors = _service.GetMonitors();
        for (int i = 0; i < monitors.Count; i++)
        {
            Assert.Equal(i, monitors[i].Index);
        }
    }

    [RequiresInteractiveDesktopFact]
    public void CaptureMonitor_FirstMonitor_ReturnsValidPng()
    {
        byte[] result = _service.CaptureMonitor(0);
        Assert.True(result.Length >= PngSignature.Length, "Result is too short to contain a PNG header.");
        Assert.Equal(PngSignature, result[..PngSignature.Length]);
    }

    [RequiresInteractiveDesktopFact]
    public void CaptureMonitor_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.CaptureMonitor(999));
    }

    [RequiresInteractiveDesktopFact]
    public void CaptureMonitor_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.CaptureMonitor(-1));
    }
}
