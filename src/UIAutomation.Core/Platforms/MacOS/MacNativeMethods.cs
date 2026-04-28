#if MACOS
using System.Runtime.InteropServices;
using System.Text;

namespace UIAutomation.Core.Platforms.MacOS;

internal static class MacNativeMethods
{
    internal const string ApplicationServicesLibrary = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    internal const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    internal const string ImageIOLibrary = "/System/Library/Frameworks/ImageIO.framework/ImageIO";

    internal const int KAXErrorSuccess = 0;
    internal const int KAXErrorFailure = -25200;
    internal const int KAXErrorIllegalArgument = -25201;
    internal const int KAXErrorInvalidUIElement = -25202;
    internal const int KAXErrorInvalidUIElementObserver = -25203;
    internal const int KAXErrorCannotComplete = -25204;
    internal const int KAXErrorAttributeUnsupported = -25205;
    internal const int KAXErrorActionUnsupported = -25206;
    internal const int KAXErrorNotificationUnsupported = -25207;
    internal const int KAXErrorNotImplemented = -25208;
    internal const int KAXErrorAPIDisabled = -25211;
    internal const int KAXErrorNoValue = -25212;

    internal const uint KCFStringEncodingUtf8 = 0x08000100;
    internal const int KCFNumberIntType = 9;
    internal const int KCFNumberDoubleType = 13;

    internal const int KAXValueCGPointType = 1;
    internal const int KAXValueCGSizeType = 2;
    internal const int KAXValueCGRectType = 3;

    internal const uint KCGHIDEventTap = 0;
    internal const uint KCGEventLeftMouseDown = 1;
    internal const uint KCGEventLeftMouseUp = 2;
    internal const uint KCGEventKeyDown = 10;
    internal const uint KCGEventKeyUp = 11;
    internal const uint KCGMouseButtonLeft = 0;
    internal const int KCGScrollEventUnitLine = 1;

    internal const uint KCGWindowListOptionAll = 0;
    internal const uint KCGWindowListOptionOnScreenOnly = 1;
    internal const uint KCGWindowListExcludeDesktopElements = 16;
    internal const uint KCGNullWindowID = 0;
    internal const uint KCGWindowImageDefault = 0;

    private static readonly Lazy<IntPtr> CoreFoundationHandle = new(() => NativeLibrary.Load(CoreFoundationLibrary));

    internal static IntPtr KCFBooleanTrue => Marshal.ReadIntPtr(NativeLibrary.GetExport(CoreFoundationHandle.Value, "kCFBooleanTrue"));
    internal static IntPtr KCFBooleanFalse => Marshal.ReadIntPtr(NativeLibrary.GetExport(CoreFoundationHandle.Value, "kCFBooleanFalse"));
    internal static IntPtr KAXTrustedCheckOptionPrompt => Marshal.ReadIntPtr(NativeLibrary.GetExport(
        NativeLibrary.Load(ApplicationServicesLibrary),
        "kAXTrustedCheckOptionPrompt"));

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFRetain(IntPtr cf);

    [DllImport(CoreFoundationLibrary)]
    internal static extern void CFRelease(IntPtr cf);

    [DllImport(CoreFoundationLibrary)]
    internal static extern nint CFHash(IntPtr cf);

    [DllImport(CoreFoundationLibrary)]
    internal static extern nint CFGetTypeID(IntPtr cf);

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string cStr, uint encoding);

    [DllImport(CoreFoundationLibrary)]
    internal static extern nint CFStringGetLength(IntPtr theString);

    [DllImport(CoreFoundationLibrary)]
    internal static extern nint CFStringGetMaximumSizeForEncoding(nint length, uint encoding);

    [DllImport(CoreFoundationLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool CFStringGetCString(IntPtr theString, [Out] byte[] buffer, nint bufferSize, uint encoding);

    [DllImport(CoreFoundationLibrary)]
    internal static extern nint CFStringGetTypeID();

    [DllImport(CoreFoundationLibrary)]
    internal static extern nint CFBooleanGetTypeID();

    [DllImport(CoreFoundationLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool CFBooleanGetValue(IntPtr boolean);

    [DllImport(CoreFoundationLibrary)]
    internal static extern nint CFNumberGetTypeID();

    [DllImport(CoreFoundationLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool CFNumberGetValue(IntPtr number, int theType, out int valuePtr);

    [DllImport(CoreFoundationLibrary, EntryPoint = "CFNumberGetValue")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool CFNumberGetDoubleValue(IntPtr number, int theType, out double valuePtr);

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFNumberCreate(IntPtr allocator, int theType, ref int valuePtr);

    [DllImport(CoreFoundationLibrary, EntryPoint = "CFNumberCreate")]
    internal static extern IntPtr CFNumberCreateDouble(IntPtr allocator, int theType, ref double valuePtr);

    [DllImport(CoreFoundationLibrary)]
    internal static extern nint CFArrayGetTypeID();

    [DllImport(CoreFoundationLibrary)]
    internal static extern nint CFArrayGetCount(IntPtr theArray);

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFArrayGetValueAtIndex(IntPtr theArray, nint idx);

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFDictionaryGetValue(IntPtr theDict, IntPtr key);

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFDictionaryCreate(
        IntPtr allocator,
        IntPtr[] keys,
        IntPtr[] values,
        nint numValues,
        IntPtr keyCallBacks,
        IntPtr valueCallBacks);

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFDataCreateMutable(IntPtr allocator, nint capacity);

    [DllImport(CoreFoundationLibrary)]
    internal static extern nint CFDataGetLength(IntPtr theData);

    [DllImport(CoreFoundationLibrary)]
    internal static extern IntPtr CFDataGetBytePtr(IntPtr theData);

    [DllImport(ApplicationServicesLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool AXIsProcessTrusted();

    [DllImport(ApplicationServicesLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool AXIsProcessTrustedWithOptions(IntPtr options);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern IntPtr AXUIElementCreateApplication(int pid);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern IntPtr AXUIElementCreateSystemWide();

    [DllImport(ApplicationServicesLibrary)]
    internal static extern nint AXUIElementGetTypeID();

    [DllImport(ApplicationServicesLibrary)]
    internal static extern int AXUIElementGetPid(IntPtr element, out int pid);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern int AXUIElementCopyAttributeValue(IntPtr element, IntPtr attribute, out IntPtr value);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern int AXUIElementSetAttributeValue(IntPtr element, IntPtr attribute, IntPtr value);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern int AXUIElementIsAttributeSettable(
        IntPtr element,
        IntPtr attribute,
        [MarshalAs(UnmanagedType.I1)] out bool settable);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern int AXUIElementCopyActionNames(IntPtr element, out IntPtr names);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern int AXUIElementPerformAction(IntPtr element, IntPtr action);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern nint AXValueGetTypeID();

    [DllImport(ApplicationServicesLibrary)]
    internal static extern int AXValueGetType(IntPtr value);

    [DllImport(ApplicationServicesLibrary, EntryPoint = "AXValueGetValue")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool AXValueGetCGPointValue(IntPtr value, int theType, out CGPoint point);

    [DllImport(ApplicationServicesLibrary, EntryPoint = "AXValueGetValue")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool AXValueGetCGSizeValue(IntPtr value, int theType, out CGSize size);

    [DllImport(ApplicationServicesLibrary, EntryPoint = "AXValueGetValue")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool AXValueGetCGRectValue(IntPtr value, int theType, out CGRect rect);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern IntPtr AXValueCreate(int theType, ref CGPoint valuePtr);

    [DllImport(ApplicationServicesLibrary, EntryPoint = "AXValueCreate")]
    internal static extern IntPtr AXValueCreateCGSize(int theType, ref CGSize valuePtr);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern IntPtr CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern IntPtr CGWindowListCreateImage(
        CGRect screenBounds,
        uint listOption,
        uint windowID,
        uint imageOption);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern int CGGetActiveDisplayList(uint maxDisplays, [Out] uint[] activeDisplays, out uint displayCount);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern CGRect CGDisplayBounds(uint display);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern uint CGMainDisplayID();

    [DllImport(ApplicationServicesLibrary)]
    internal static extern IntPtr CGDisplayCreateImage(uint display);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern IntPtr CGEventCreateMouseEvent(IntPtr source, uint mouseType, CGPoint mouseCursorPosition, uint mouseButton);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey, [MarshalAs(UnmanagedType.I1)] bool keyDown);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern void CGEventKeyboardSetUnicodeString(IntPtr @event, nint stringLength, [In] char[] unicodeString);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern IntPtr CGEventCreateScrollWheelEvent(
        IntPtr source,
        int units,
        uint wheelCount,
        int wheel1,
        int wheel2);

    [DllImport(ApplicationServicesLibrary)]
    internal static extern void CGEventPost(uint tap, IntPtr @event);

    [DllImport(ImageIOLibrary)]
    internal static extern IntPtr CGImageDestinationCreateWithData(IntPtr data, IntPtr type, nint count, IntPtr options);

    [DllImport(ImageIOLibrary)]
    internal static extern void CGImageDestinationAddImage(IntPtr idst, IntPtr image, IntPtr properties);

    [DllImport(ImageIOLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool CGImageDestinationFinalize(IntPtr idst);

    internal static IntPtr CreateCFString(string value)
    {
        var result = CFStringCreateWithCString(IntPtr.Zero, value, KCFStringEncodingUtf8);
        if (result == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Unable to create CFString for '{value}'.");
        }

        return result;
    }

    internal static string? CFStringToString(IntPtr cfString)
    {
        if (cfString == IntPtr.Zero)
        {
            return null;
        }

        var length = CFStringGetLength(cfString);
        var maxSize = CFStringGetMaximumSizeForEncoding(length, KCFStringEncodingUtf8) + 1;
        var buffer = new byte[(int)maxSize];

        if (!CFStringGetCString(cfString, buffer, maxSize, KCFStringEncodingUtf8))
        {
            return null;
        }

        int stringLength = Array.IndexOf(buffer, (byte)0);
        if (stringLength < 0)
        {
            stringLength = buffer.Length;
        }

        return Encoding.UTF8.GetString(buffer, 0, stringLength);
    }

    internal static bool IsType(IntPtr value, nint typeId) =>
        value != IntPtr.Zero && CFGetTypeID(value) == typeId;

    [StructLayout(LayoutKind.Sequential)]
    internal struct CGPoint
    {
        public double X;
        public double Y;

        public CGPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CGSize
    {
        public double Width;
        public double Height;

        public CGSize(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CGRect
    {
        public CGPoint Origin;
        public CGSize Size;

        public CGRect(double x, double y, double width, double height)
        {
            Origin = new CGPoint(x, y);
            Size = new CGSize(width, height);
        }
    }
}
#endif
