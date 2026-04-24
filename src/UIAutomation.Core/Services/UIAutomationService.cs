using System.Runtime.InteropServices;
using System.Windows.Automation;
using UIAutomation.Core.Models;

namespace UIAutomation.Core.Services;

/// <summary>
/// Implementation of IUIAutomationService using System.Windows.Automation.
/// All public methods are marshaled to an STA thread to satisfy COM requirements.
/// </summary>
public sealed class UIAutomationService : IUIAutomationService
{
    private readonly ElementCache _cache;
    private static readonly object PhysicalInputLock = new();

    public UIAutomationService(ElementCache cache)
    {
        _cache = cache;
    }

    public List<ElementInfo> ListWindows() => RunOnSta(() =>
    {
        var root = AutomationElement.RootElement;
        var windows = root.FindAll(
            TreeScope.Children,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

        var results = new List<ElementInfo>();
        foreach (AutomationElement window in windows)
        {
            try
            {
                if (window.Current.IsOffscreen)
                    continue;

                results.Add(ToElementInfo(window));
            }
            catch (ElementNotAvailableException)
            {
                // Window disappeared between enumeration and access
            }
        }

        return results;
    });

    public List<ElementInfo> FindElements(string parentElementId, string? name = null, string? automationId = null, string? controlType = null) => RunOnSta(() =>
    {
        var parent = GetCachedElement(parentElementId);

        var conditions = new List<Condition>();

        if (!string.IsNullOrEmpty(name))
            conditions.Add(new PropertyCondition(AutomationElement.NameProperty, name));

        if (!string.IsNullOrEmpty(automationId))
            conditions.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));

        if (!string.IsNullOrEmpty(controlType))
        {
            var ct = ParseControlType(controlType);
            if (ct != null)
                conditions.Add(new PropertyCondition(AutomationElement.ControlTypeProperty, ct));
        }

        Condition searchCondition = conditions.Count switch
        {
            0 => Condition.TrueCondition,
            1 => conditions[0],
            _ => new AndCondition(conditions.ToArray())
        };

        var found = parent.FindAll(TreeScope.Descendants, searchCondition);
        var results = new List<ElementInfo>();

        foreach (AutomationElement element in found)
        {
            try
            {
                results.Add(ToElementInfo(element));
            }
            catch (ElementNotAvailableException) { }
        }

        return results;
    });

    public ElementInfo? GetElementInfo(string elementId) => RunOnSta(() =>
    {
        if (!_cache.TryGet(elementId, out var element) || element == null)
            return null;

        try
        {
            return ToElementInfo(element, elementId);
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    });

    public List<ElementInfo> GetElementTree(string elementId, int maxDepth = 3) => RunOnSta(() =>
    {
        var root = GetCachedElement(elementId);
        var walker = TreeWalker.ControlViewWalker;
        var results = new List<ElementInfo>();

        try
        {
            var child = walker.GetFirstChild(root);
            while (child != null)
            {
                try
                {
                    var info = ToElementInfo(child);
                    if (maxDepth > 1)
                    {
                        info.Children = GetChildrenRecursive(child, walker, maxDepth - 1);
                    }
                    results.Add(info);
                }
                catch (ElementNotAvailableException) { }

                child = walker.GetNextSibling(child);
            }
        }
        catch (ElementNotAvailableException) { }

        return results;
    });

    public void InvokeElement(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
        {
            ((InvokePattern)pattern).Invoke();
        }
        else
        {
            throw new InvalidOperationException(
                $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support InvokePattern.");
        }
    });

    public void SetValue(string elementId, string value) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
        {
            ((ValuePattern)pattern).SetValue(value);
        }
        else
        {
            throw new InvalidOperationException(
                $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support ValuePattern.");
        }
    });

    public string GetValue(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
        {
            return ((ValuePattern)pattern).Current.Value;
        }

        // Fall back to the Name property
        return element.Current.Name;
    });

    public string ToggleElement(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var pattern))
        {
            var toggle = (TogglePattern)pattern;
            toggle.Toggle();
            return toggle.Current.ToggleState.ToString();
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support TogglePattern.");
    });

    public void ClickAtPoint(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);
        var current = element.Current;

        if (current.IsOffscreen)
        {
            throw new InvalidOperationException(
                $"Element '{elementId}' (Name=\"{current.Name}\") is offscreen and cannot be clicked.");
        }

        // Prefer GetClickablePoint; fall back to bounding rectangle center.
        System.Windows.Point clickPoint;
        if (element.TryGetClickablePoint(out var point))
        {
            clickPoint = point;
        }
        else
        {
            var rect = current.BoundingRectangle;
            if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0)
            {
                throw new InvalidOperationException(
                    $"Element '{elementId}' (Name=\"{current.Name}\") has an empty bounding rectangle.");
            }

            clickPoint = new System.Windows.Point(
                rect.X + rect.Width / 2,
                rect.Y + rect.Height / 2);
        }

        int screenX = (int)clickPoint.X;
        int screenY = (int)clickPoint.Y;

        // Best-effort focus before clicking.
        try { element.SetFocus(); }
        catch { /* not all elements support focus */ }

        // Serialize all physical input to prevent cursor races.
        lock (PhysicalInputLock)
        {
            NativeMethods.GetCursorPos(out var previousPos);

            try
            {
                NativeMethods.SetCursorPos(screenX, screenY);

                var inputs = new NativeMethods.INPUT[2];

                inputs[0].type = NativeMethods.INPUT_MOUSE;
                inputs[0].union.mi.dwFlags = NativeMethods.MOUSEEVENTF_LEFTDOWN;

                inputs[1].type = NativeMethods.INPUT_MOUSE;
                inputs[1].union.mi.dwFlags = NativeMethods.MOUSEEVENTF_LEFTUP;

                uint sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
                if (sent != inputs.Length)
                {
                    throw new InvalidOperationException(
                        $"SendInput failed: sent {sent} of {inputs.Length} events (error {Marshal.GetLastWin32Error()}).");
                }
            }
            finally
            {
                NativeMethods.SetCursorPos(previousPos.X, previousPos.Y);
            }
        }
    });

    public ElementQueryResult QueryElements(string rootElementId, ElementQueryOptions? options = null) => RunOnSta(() =>
    {
        var root = GetCachedElement(rootElementId);
        options ??= new ElementQueryOptions();
        var filter = options.Filter;
        var walker = TreeWalker.ControlViewWalker;
        int scannedCount = 0;
        int matchedCount = 0;

        if (options.Flatten)
        {
            var results = new List<ElementInfo>();
            CollectMatchingFlat(root, walker, filter, options.MaxDepth, options.MaxResults, results, ref scannedCount, ref matchedCount);
            return new ElementQueryResult
            {
                Elements = results,
                MatchedCount = matchedCount,
                ScannedCount = scannedCount,
                Truncated = matchedCount > options.MaxResults,
            };
        }
        else
        {
            var tree = CollectMatchingTree(root, walker, filter, options.MaxDepth, options.MaxResults, ref scannedCount, ref matchedCount);
            return new ElementQueryResult
            {
                Elements = tree,
                MatchedCount = matchedCount,
                ScannedCount = scannedCount,
                Truncated = matchedCount > options.MaxResults,
            };
        }
    });

    // --- Query helpers ---

    /// <summary>
    /// Walks the tree and collects matching elements into a flat list.
    /// Only matching elements are cached (via ToElementInfo).
    /// </summary>
    private void CollectMatchingFlat(
        AutomationElement parent,
        TreeWalker walker,
        ElementFilter? filter,
        int? maxDepth,
        int maxResults,
        List<ElementInfo> results,
        ref int scannedCount,
        ref int matchedCount)
    {
        if (maxDepth.HasValue && maxDepth.Value <= 0)
            return;

        try
        {
            var child = walker.GetFirstChild(parent);
            while (child != null)
            {
                scannedCount++;

                try
                {
                    if (MatchesFilter(child, filter))
                    {
                        matchedCount++;
                        if (results.Count < maxResults)
                        {
                            results.Add(ToElementInfo(child));
                        }
                    }

                    // Continue walking even if this node didn't match (descendants might)
                    CollectMatchingFlat(child, walker, filter, maxDepth.HasValue ? maxDepth.Value - 1 : null, maxResults, results, ref scannedCount, ref matchedCount);
                }
                catch (ElementNotAvailableException) { }

                child = walker.GetNextSibling(child);
            }
        }
        catch (ElementNotAvailableException) { }
    }

    /// <summary>
    /// Walks the tree and returns a pruned tree containing only matching elements
    /// and their ancestor containers. Non-matching leaves are excluded.
    /// </summary>
    private List<ElementInfo> CollectMatchingTree(
        AutomationElement parent,
        TreeWalker walker,
        ElementFilter? filter,
        int? maxDepth,
        int maxResults,
        ref int scannedCount,
        ref int matchedCount)
    {
        var results = new List<ElementInfo>();

        if (maxDepth.HasValue && maxDepth.Value <= 0)
            return results;

        try
        {
            var child = walker.GetFirstChild(parent);
            while (child != null)
            {
                scannedCount++;

                try
                {
                    bool thisMatches = MatchesFilter(child, filter);
                    if (thisMatches)
                        matchedCount++;

                    // Recurse to find matching descendants
                    var childResults = CollectMatchingTree(child, walker, filter, maxDepth.HasValue ? maxDepth.Value - 1 : null, maxResults, ref scannedCount, ref matchedCount);

                    if (thisMatches || childResults.Count > 0)
                    {
                        // Include this element if it matches or has matching descendants
                        var info = ToElementInfo(child);
                        info.Children = childResults.Count > 0 ? childResults : null;
                        results.Add(info);
                    }
                }
                catch (ElementNotAvailableException) { }

                child = walker.GetNextSibling(child);
            }
        }
        catch (ElementNotAvailableException) { }

        return results;
    }

    /// <summary>
    /// Tests whether an AutomationElement matches the given filter using cheap
    /// property reads from AutomationElement.Current. Only calls GetSupportedPatterns()
    /// if the filter requires it and all other criteria have passed.
    /// </summary>
    private static bool MatchesFilter(AutomationElement element, ElementFilter? filter)
    {
        if (filter is null || filter.IsEmpty)
            return true;

        var current = element.Current;

        // Check cheap properties first
        if (filter.IsEnabled.HasValue && current.IsEnabled != filter.IsEnabled.Value)
            return false;

        if (filter.IsOffscreen.HasValue && current.IsOffscreen != filter.IsOffscreen.Value)
            return false;

        if (filter.ControlTypes is { Length: > 0 })
        {
            var elementControlType = current.ControlType.ProgrammaticName.Replace("ControlType.", "");
            if (!filter.ControlTypes.Any(ct => string.Equals(ct, elementControlType, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        if (!string.IsNullOrEmpty(filter.NameContains))
        {
            if (current.Name is null || !current.Name.Contains(filter.NameContains, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (!string.IsNullOrEmpty(filter.AutomationIdContains))
        {
            if (current.AutomationId is null || !current.AutomationId.Contains(filter.AutomationIdContains, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (!string.IsNullOrEmpty(filter.ClassNameContains))
        {
            if (current.ClassName is null || !current.ClassName.Contains(filter.ClassNameContains, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Expensive check last: supported patterns
        if (filter.SupportedPatterns is { Length: > 0 })
        {
            var patterns = GetSupportedPatternNames(element);
            if (!filter.SupportedPatterns.Any(fp => patterns.Any(p => string.Equals(p, fp, StringComparison.OrdinalIgnoreCase))))
                return false;
        }

        return true;
    }

    // --- STA thread helper ---

    /// <summary>
    /// Runs the given function on an STA thread if the current thread is not STA.
    /// UI Automation COM calls require STA apartment state.
    /// </summary>
    private static T RunOnSta<T>(Func<T> func)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            return func();

        T result = default!;
        Exception? exception = null;

        var thread = new Thread(() =>
        {
            try { result = func(); }
            catch (Exception ex) { exception = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();

        return result;
    }

    private static void RunOnSta(Action action)
    {
        RunOnSta<object?>(() => { action(); return null; });
    }

    // --- Private helpers ---

    private AutomationElement GetCachedElement(string elementId)
    {
        if (!_cache.TryGet(elementId, out var element) || element == null)
            throw new KeyNotFoundException($"No cached element found with ID '{elementId}'. Use list_windows or find_element first.");

        return element;
    }

    private ElementInfo ToElementInfo(AutomationElement element, string? existingId = null)
    {
        var id = existingId ?? _cache.GetOrAdd(element);
        var current = element.Current;
        var patterns = GetSupportedPatternNames(element);

        return new ElementInfo
        {
            ElementId = id,
            Name = current.Name ?? "",
            AutomationId = current.AutomationId ?? "",
            ControlType = current.ControlType.ProgrammaticName.Replace("ControlType.", ""),
            ClassName = current.ClassName ?? "",
            LocalizedControlType = current.LocalizedControlType ?? "",
            BoundingRectangle = new BoundsInfo
            {
                X = SanitizeDouble(current.BoundingRectangle.X),
                Y = SanitizeDouble(current.BoundingRectangle.Y),
                Width = SanitizeDouble(current.BoundingRectangle.Width),
                Height = SanitizeDouble(current.BoundingRectangle.Height),
            },
            IsEnabled = current.IsEnabled,
            IsOffscreen = current.IsOffscreen,
            ProcessId = current.ProcessId,
            SupportedPatterns = patterns,
        };
    }

    private static string[] GetSupportedPatternNames(AutomationElement element)
    {
        try
        {
            var patterns = element.GetSupportedPatterns();
            return patterns
                .Select(p => Automation.PatternName(p))
                .Where(n => !string.IsNullOrEmpty(n))
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static double SanitizeDouble(double value) =>
        double.IsInfinity(value) || double.IsNaN(value) ? 0 : value;

    private List<ElementInfo> GetChildrenRecursive(AutomationElement parent, TreeWalker walker, int remainingDepth)
    {
        var children = new List<ElementInfo>();
        try
        {
            var child = walker.GetFirstChild(parent);
            while (child != null)
            {
                try
                {
                    var info = ToElementInfo(child);
                    if (remainingDepth > 1)
                    {
                        info.Children = GetChildrenRecursive(child, walker, remainingDepth - 1);
                    }
                    children.Add(info);
                }
                catch (ElementNotAvailableException) { }

                child = walker.GetNextSibling(child);
            }
        }
        catch (ElementNotAvailableException) { }

        return children;
    }

    private static readonly Dictionary<string, ControlType> ControlTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Button"] = ControlType.Button,
        ["Calendar"] = ControlType.Calendar,
        ["CheckBox"] = ControlType.CheckBox,
        ["ComboBox"] = ControlType.ComboBox,
        ["Custom"] = ControlType.Custom,
        ["DataGrid"] = ControlType.DataGrid,
        ["DataItem"] = ControlType.DataItem,
        ["Document"] = ControlType.Document,
        ["Edit"] = ControlType.Edit,
        ["Group"] = ControlType.Group,
        ["Header"] = ControlType.Header,
        ["HeaderItem"] = ControlType.HeaderItem,
        ["Hyperlink"] = ControlType.Hyperlink,
        ["Image"] = ControlType.Image,
        ["List"] = ControlType.List,
        ["ListItem"] = ControlType.ListItem,
        ["Menu"] = ControlType.Menu,
        ["MenuBar"] = ControlType.MenuBar,
        ["MenuItem"] = ControlType.MenuItem,
        ["Pane"] = ControlType.Pane,
        ["ProgressBar"] = ControlType.ProgressBar,
        ["RadioButton"] = ControlType.RadioButton,
        ["ScrollBar"] = ControlType.ScrollBar,
        ["Separator"] = ControlType.Separator,
        ["Slider"] = ControlType.Slider,
        ["Spinner"] = ControlType.Spinner,
        ["SplitButton"] = ControlType.SplitButton,
        ["StatusBar"] = ControlType.StatusBar,
        ["Tab"] = ControlType.Tab,
        ["TabItem"] = ControlType.TabItem,
        ["Table"] = ControlType.Table,
        ["Text"] = ControlType.Text,
        ["Thumb"] = ControlType.Thumb,
        ["TitleBar"] = ControlType.TitleBar,
        ["ToolBar"] = ControlType.ToolBar,
        ["ToolTip"] = ControlType.ToolTip,
        ["Tree"] = ControlType.Tree,
        ["TreeItem"] = ControlType.TreeItem,
        ["Window"] = ControlType.Window,
    };

    private static ControlType? ParseControlType(string name)
    {
        return ControlTypeMap.TryGetValue(name, out var ct) ? ct : null;
    }

    /// <summary>
    /// Win32 P/Invoke declarations for physical mouse input simulation.
    /// </summary>
    private static class NativeMethods
    {
        internal const int INPUT_MOUSE = 0;
        internal const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        internal const uint MOUSEEVENTF_LEFTUP = 0x0004;

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
    }
}
