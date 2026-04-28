#if MACOS
using System.Runtime.InteropServices;
using UIAutomation.Core.Models;
using UIAutomation.Core.Services;
using UIAutomation.Core.Backends;

namespace UIAutomation.Core.Platforms.MacOS;

/// <summary>
/// Captures macOS displays using CoreGraphics and encodes images with ImageIO.
/// </summary>
public sealed class MacScreenCaptureBackend : IScreenCaptureBackend
{
    public byte[] CaptureScreen()
    {
        var displays = GetDisplayInfos();
        if (displays.Count == 0)
        {
            throw new InvalidOperationException("No active macOS displays were found.");
        }

        var bounds = UnionBounds(displays.Select(d => d.Bounds));
        var image = MacNativeMethods.CGWindowListCreateImage(
            bounds,
            MacNativeMethods.KCGWindowListOptionOnScreenOnly,
            MacNativeMethods.KCGNullWindowID,
            MacNativeMethods.KCGWindowImageDefault);

        if (image == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Unable to capture the screen. macOS may require Screen Recording permission for this process.");
        }

        try
        {
            return EncodePng(image);
        }
        finally
        {
            MacNativeMethods.CFRelease(image);
        }
    }

    public byte[] CaptureMonitor(int monitorIndex)
    {
        var displays = GetDisplayInfos();
        var target = displays.FirstOrDefault(d => d.Info.Index == monitorIndex)
            ?? throw new ArgumentOutOfRangeException(
                nameof(monitorIndex),
                monitorIndex,
                $"Monitor index {monitorIndex} not found. Available indices: 0-{displays.Count - 1}.");

        var image = MacNativeMethods.CGDisplayCreateImage(target.DisplayId);
        if (image == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Unable to capture monitor {monitorIndex}. macOS may require Screen Recording permission for this process.");
        }

        try
        {
            return EncodePng(image);
        }
        finally
        {
            MacNativeMethods.CFRelease(image);
        }
    }

    public IReadOnlyList<MonitorInfo> GetMonitors() =>
        GetDisplayInfos().Select(d => d.Info).ToArray();

    private static List<DisplayInfo> GetDisplayInfos()
    {
        var displayIds = new uint[32];
        int error = MacNativeMethods.CGGetActiveDisplayList((uint)displayIds.Length, displayIds, out uint displayCount);
        if (error != 0)
        {
            throw new InvalidOperationException($"CGGetActiveDisplayList failed with error {error}.");
        }

        var mainDisplayId = MacNativeMethods.CGMainDisplayID();
        var displays = new List<DisplayInfo>();

        for (int i = 0; i < displayCount; i++)
        {
            uint displayId = displayIds[i];
            var bounds = MacNativeMethods.CGDisplayBounds(displayId);
            displays.Add(new DisplayInfo(
                displayId,
                bounds,
                new MonitorInfo
                {
                    Index = i,
                    DeviceName = $"CGDisplay-{displayId}",
                    IsPrimary = displayId == mainDisplayId,
                    Left = (int)Math.Round(bounds.Origin.X),
                    Top = (int)Math.Round(bounds.Origin.Y),
                    Width = (int)Math.Round(bounds.Size.Width),
                    Height = (int)Math.Round(bounds.Size.Height),
                }));
        }

        return displays;
    }

    private static MacNativeMethods.CGRect UnionBounds(IEnumerable<MacNativeMethods.CGRect> rects)
    {
        double left = double.PositiveInfinity;
        double top = double.PositiveInfinity;
        double right = double.NegativeInfinity;
        double bottom = double.NegativeInfinity;

        foreach (var rect in rects)
        {
            left = Math.Min(left, rect.Origin.X);
            top = Math.Min(top, rect.Origin.Y);
            right = Math.Max(right, rect.Origin.X + rect.Size.Width);
            bottom = Math.Max(bottom, rect.Origin.Y + rect.Size.Height);
        }

        return new MacNativeMethods.CGRect(left, top, right - left, bottom - top);
    }

    private static byte[] EncodePng(IntPtr image)
    {
        var data = MacNativeMethods.CFDataCreateMutable(IntPtr.Zero, 0);
        if (data == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to allocate CFMutableData for PNG encoding.");
        }

        var pngType = MacNativeMethods.CreateCFString("public.png");
        IntPtr destination = IntPtr.Zero;

        try
        {
            destination = MacNativeMethods.CGImageDestinationCreateWithData(data, pngType, 1, IntPtr.Zero);
            if (destination == IntPtr.Zero)
            {
                throw new InvalidOperationException("Unable to create ImageIO PNG destination.");
            }

            MacNativeMethods.CGImageDestinationAddImage(destination, image, IntPtr.Zero);
            if (!MacNativeMethods.CGImageDestinationFinalize(destination))
            {
                throw new InvalidOperationException("ImageIO failed to encode screenshot as PNG.");
            }

            var length = MacNativeMethods.CFDataGetLength(data);
            var bytesPointer = MacNativeMethods.CFDataGetBytePtr(data);
            if (length <= 0 || bytesPointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("ImageIO produced an empty PNG.");
            }

            var bytes = new byte[(int)length];
            Marshal.Copy(bytesPointer, bytes, 0, bytes.Length);
            return bytes;
        }
        finally
        {
            if (destination != IntPtr.Zero)
            {
                MacNativeMethods.CFRelease(destination);
            }

            MacNativeMethods.CFRelease(pngType);
            MacNativeMethods.CFRelease(data);
        }
    }

    private sealed record DisplayInfo(uint DisplayId, MacNativeMethods.CGRect Bounds, MonitorInfo Info);
}
#endif
