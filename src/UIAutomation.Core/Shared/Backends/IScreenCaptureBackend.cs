using UIAutomation.Core.Models;

namespace UIAutomation.Core.Backends;

/// <summary>
/// Platform-specific screen capture contract used by the shared front-end service.
/// </summary>
public interface IScreenCaptureBackend
{
    byte[] CaptureScreen();
    byte[] CaptureMonitor(int monitorIndex);
    IReadOnlyList<MonitorInfo> GetMonitors();
}
