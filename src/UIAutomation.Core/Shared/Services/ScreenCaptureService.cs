using UIAutomation.Core.Models;
using UIAutomation.Core.Backends;

namespace UIAutomation.Core.Services;

/// <summary>
/// Shared front-end service that delegates screen capture operations to the active platform backend.
/// </summary>
public sealed class ScreenCaptureService : IScreenCaptureService
{
    private readonly IScreenCaptureBackend _backend;

    public ScreenCaptureService(IScreenCaptureBackend backend)
    {
        _backend = backend;
    }

    public byte[] CaptureScreen() => _backend.CaptureScreen();

    public byte[] CaptureMonitor(int monitorIndex) => _backend.CaptureMonitor(monitorIndex);

    public IReadOnlyList<MonitorInfo> GetMonitors() => _backend.GetMonitors();
}
