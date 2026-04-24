using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace UIAutomation.Core.Services;

/// <summary>
/// Captures the full virtual screen using GDI+.
/// </summary>
public sealed class ScreenCaptureService : IScreenCaptureService
{
    /// <inheritdoc />
    public byte[] CaptureScreen()
    {
        int left = (int)SystemParameters.VirtualScreenLeft;
        int top = (int)SystemParameters.VirtualScreenTop;
        int width = (int)SystemParameters.VirtualScreenWidth;
        int height = (int)SystemParameters.VirtualScreenHeight;

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
