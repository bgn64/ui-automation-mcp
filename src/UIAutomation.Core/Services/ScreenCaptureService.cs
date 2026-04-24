using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using UIAutomation.Core.Models;

namespace UIAutomation.Core.Services;

/// <summary>
/// Captures the screen using GDI+ with DPI-aware physical pixel coordinates.
/// </summary>
public sealed class ScreenCaptureService : IScreenCaptureService
{
    /// <inheritdoc />
    public byte[] CaptureScreen()
    {
        return WithPerMonitorDpiAwareness(() =>
        {
            int left = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
            int top = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
            int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
            int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

            return CaptureRegion(left, top, width, height);
        });
    }

    /// <inheritdoc />
    public byte[] CaptureMonitor(int monitorIndex)
    {
        var monitors = GetMonitors();
        var target = monitors.FirstOrDefault(m => m.Index == monitorIndex)
            ?? throw new ArgumentOutOfRangeException(
                nameof(monitorIndex),
                monitorIndex,
                $"Monitor index {monitorIndex} not found. Available indices: 0–{monitors.Count - 1}.");

        return WithPerMonitorDpiAwareness(() =>
            CaptureRegion(target.Left, target.Top, target.Width, target.Height));
    }

    /// <inheritdoc />
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        return WithPerMonitorDpiAwareness(() =>
        {
            var monitors = new List<MonitorInfo>();
            int index = 0;

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref NativeMethods.RECT _, IntPtr _) =>
            {
                var info = new NativeMethods.MONITORINFOEX();
                info.cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFOEX>();

                if (NativeMethods.GetMonitorInfo(hMonitor, ref info))
                {
                    monitors.Add(new MonitorInfo
                    {
                        Index = index++,
                        DeviceName = info.szDevice,
                        IsPrimary = (info.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0,
                        Left = info.rcMonitor.Left,
                        Top = info.rcMonitor.Top,
                        Width = info.rcMonitor.Right - info.rcMonitor.Left,
                        Height = info.rcMonitor.Bottom - info.rcMonitor.Top,
                    });
                }

                return true; // continue enumeration
            }, IntPtr.Zero);

            return monitors;
        });
    }

    /// <summary>
    /// Captures a rectangular region of the screen and returns PNG bytes.
    /// Must be called within a DPI-aware context.
    /// </summary>
    private static byte[] CaptureRegion(int left, int top, int width, int height)
    {
        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(left, top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    /// <summary>
    /// Executes <paramref name="action"/> with Per-Monitor V2 DPI awareness on the current thread,
    /// restoring the previous context afterward.
    /// </summary>
    private static T WithPerMonitorDpiAwareness<T>(Func<T> action)
    {
        IntPtr previous = NativeMethods.SetThreadDpiAwarenessContext(
            NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        try
        {
            return action();
        }
        finally
        {
            if (previous != IntPtr.Zero)
            {
                NativeMethods.SetThreadDpiAwarenessContext(previous);
            }
        }
    }
}
