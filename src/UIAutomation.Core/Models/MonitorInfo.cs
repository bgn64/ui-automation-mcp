namespace UIAutomation.Core.Models;

/// <summary>
/// Describes a physical display monitor with its bounds in physical pixels.
/// </summary>
public sealed record MonitorInfo
{
    /// <summary>Zero-based index assigned during enumeration.</summary>
    public required int Index { get; init; }

    /// <summary>Win32 device name (e.g. \\.\DISPLAY1).</summary>
    public required string DeviceName { get; init; }

    /// <summary>Whether this is the primary monitor.</summary>
    public required bool IsPrimary { get; init; }

    /// <summary>Left edge in physical pixels (virtual screen coordinates).</summary>
    public required int Left { get; init; }

    /// <summary>Top edge in physical pixels (virtual screen coordinates).</summary>
    public required int Top { get; init; }

    /// <summary>Width in physical pixels.</summary>
    public required int Width { get; init; }

    /// <summary>Height in physical pixels.</summary>
    public required int Height { get; init; }
}
