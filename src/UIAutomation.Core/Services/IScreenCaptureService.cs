namespace UIAutomation.Core.Services;

/// <summary>
/// Abstraction for capturing screen content as image data.
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// Captures the full virtual screen (all monitors) and returns PNG-encoded bytes.
    /// </summary>
    byte[] CaptureScreen();
}
