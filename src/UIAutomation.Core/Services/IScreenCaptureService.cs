using UIAutomation.Core.Models;

namespace UIAutomation.Core.Services;

/// <summary>
/// Abstraction for capturing screen content as image data.
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// Captures the full virtual screen (all monitors) and returns PNG-encoded bytes.
    /// Uses physical pixel dimensions (DPI-aware).
    /// </summary>
    byte[] CaptureScreen();

    /// <summary>
    /// Captures a single monitor and returns PNG-encoded bytes.
    /// </summary>
    /// <param name="monitorIndex">Zero-based monitor index from <see cref="GetMonitors"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="monitorIndex"/> does not match any monitor.</exception>
    byte[] CaptureMonitor(int monitorIndex);

    /// <summary>
    /// Returns metadata for every connected monitor, in physical pixels.
    /// </summary>
    IReadOnlyList<MonitorInfo> GetMonitors();
}
