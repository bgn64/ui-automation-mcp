using System.Runtime.InteropServices;

namespace UIAutomation.Core.Platforms.Windows;

/// <summary>
/// Win32 P/Invoke declarations for physical input simulation (mouse and keyboard)
/// and DPI-aware screen capture.
/// </summary>
internal static class NativeMethods
{
    internal const int INPUT_MOUSE = 0;
    internal const int INPUT_KEYBOARD = 1;
    internal const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    internal const uint MOUSEEVENTF_LEFTUP = 0x0004;
    internal const uint KEYEVENTF_KEYUP = 0x0002;
    internal const uint KEYEVENTF_UNICODE = 0x0004;
    internal const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    // Virtual screen metrics constants for GetSystemMetrics
    internal const int SM_XVIRTUALSCREEN = 76;
    internal const int SM_YVIRTUALSCREEN = 77;
    internal const int SM_CXVIRTUALSCREEN = 78;
    internal const int SM_CYVIRTUALSCREEN = 79;

    // DPI awareness contexts
    internal static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);

    // Monitor info flags
    internal const uint MONITORINFOF_PRIMARY = 1;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public int type;
        public INPUTUNION union;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // --- DPI awareness ---

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    // --- Screen metrics ---

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int nIndex);

    // --- Monitor enumeration ---

    internal delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct MONITORINFOEX
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }
}
