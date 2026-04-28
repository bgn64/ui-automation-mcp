#if MACOS
using System.Globalization;
using UIAutomation.Core.Models;
using UIAutomation.Core.Services;
using UIAutomation.Core.Backends;

namespace UIAutomation.Core.Platforms.MacOS;

/// <summary>
/// macOS Accessibility backend for UI automation.
/// </summary>
public sealed class MacUIAutomationBackend : IUIAutomationBackend
{
    private const string AXWindows = "AXWindows";
    private const string AXChildren = "AXChildren";
    private const string AXTitle = "AXTitle";
    private const string AXDescription = "AXDescription";
    private const string AXRole = "AXRole";
    private const string AXSubrole = "AXSubrole";
    private const string AXIdentifier = "AXIdentifier";
    private const string AXEnabled = "AXEnabled";
    private const string AXFocused = "AXFocused";
    private const string AXFocusedUIElement = "AXFocusedUIElement";
    private const string AXParent = "AXParent";
    private const string AXPosition = "AXPosition";
    private const string AXSize = "AXSize";
    private const string AXValue = "AXValue";
    private const string AXSelected = "AXSelected";
    private const string AXSelectedChildren = "AXSelectedChildren";
    private const string AXMinimized = "AXMinimized";
    private const string AXFullScreen = "AXFullScreen";
    private const string AXCloseButton = "AXCloseButton";
    private const string AXMinimizeButton = "AXMinimizeButton";
    private const string AXZoomButton = "AXZoomButton";
    private const string AXRows = "AXRows";
    private const string AXColumns = "AXColumns";
    private const string AXMinValue = "AXMinValue";
    private const string AXMaxValue = "AXMaxValue";
    private const string AXIncrement = "AXIncrement";

    private const string AXPress = "AXPress";
    private const string AXRaise = "AXRaise";
    private const string AXShowMenu = "AXShowMenu";
    private const string AXConfirm = "AXConfirm";
    private const string AXCancel = "AXCancel";
    private const string AXIncrementAction = "AXIncrement";
    private const string AXDecrementAction = "AXDecrement";
    private const string AXScrollToVisible = "AXScrollToVisible";
    private const string AXExpand = "AXExpand";
    private const string AXCollapse = "AXCollapse";

    private static readonly object s_physicalInputLock = new();

    private readonly MacElementCache _cache;

    public MacUIAutomationBackend(MacElementCache cache)
    {
        _cache = cache;
    }

    public List<ElementInfo> ListWindows()
    {
        EnsureAccessibilityTrusted();

        var results = new List<ElementInfo>();
        foreach (var pid in GetOnScreenWindowProcessIds())
        {
            var app = MacNativeMethods.AXUIElementCreateApplication(pid);
            if (app == IntPtr.Zero)
            {
                continue;
            }

            try
            {
                using var windows = CopyAttribute(app, AXWindows);
                if (windows.Value == IntPtr.Zero || !MacNativeMethods.IsType(windows.Value, MacNativeMethods.CFArrayGetTypeID()))
                {
                    continue;
                }

                foreach (var window in CopyArrayItems(windows.Value))
                {
                    try
                    {
                        var info = ToElementInfo(window);
                        if (info.BoundingRectangle is { Width: > 0, Height: > 0 })
                        {
                            results.Add(info);
                        }
                    }
                    catch (ElementStaleException)
                    {
                    }
                    finally
                    {
                        MacNativeMethods.CFRelease(window);
                    }
                }
            }
            finally
            {
                MacNativeMethods.CFRelease(app);
            }
        }

        return results
            .GroupBy(w => w.ElementId, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();
    }

    public List<ElementInfo> FindElements(string parentElementId, string? name = null, string? automationId = null, string? controlType = null)
    {
        EnsureAccessibilityTrusted();
        var parent = GetCachedElement(parentElementId);
        var results = new List<ElementInfo>();

        foreach (var child in GetChildren(parent))
        {
            try
            {
                CollectDescendants(child, candidate =>
                {
                    var info = ToElementInfo(candidate);
                    if (MatchesSimpleFilter(info, name, automationId, controlType))
                    {
                        results.Add(info);
                    }
                });
            }
            finally
            {
                MacNativeMethods.CFRelease(child);
            }
        }

        return results;
    }

    public ElementInfo? GetElementInfo(string elementId)
    {
        EnsureAccessibilityTrusted();
        if (!_cache.TryGet(elementId, out var element))
        {
            return null;
        }

        try
        {
            return ToElementInfo(element, elementId);
        }
        catch (ElementStaleException)
        {
            return null;
        }
    }

    public List<ElementInfo> GetElementTree(string elementId, int maxDepth = 3)
    {
        EnsureAccessibilityTrusted();
        var root = GetCachedElement(elementId);
        return GetChildrenRecursive(root, maxDepth);
    }

    public ElementQueryResult QueryElements(string rootElementId, ElementQueryOptions? options = null)
    {
        EnsureAccessibilityTrusted();
        var root = GetCachedElement(rootElementId);
        options ??= new ElementQueryOptions();

        int scannedCount = 0;
        int matchedCount = 0;

        if (options.Flatten)
        {
            var elements = new List<ElementInfo>();
            CollectMatchingFlat(root, options.Filter, options.MaxDepth, options.MaxResults, elements, ref scannedCount, ref matchedCount);
            return new ElementQueryResult
            {
                Elements = elements,
                MatchedCount = matchedCount,
                ScannedCount = scannedCount,
                Truncated = matchedCount > options.MaxResults,
            };
        }

        var tree = CollectMatchingTree(root, options.Filter, options.MaxDepth, options.MaxResults, ref scannedCount, ref matchedCount);
        return new ElementQueryResult
        {
            Elements = tree,
            MatchedCount = matchedCount,
            ScannedCount = scannedCount,
            Truncated = matchedCount > options.MaxResults,
        };
    }

    public void InvokeElement(string elementId) =>
        PerformRequiredAction(GetCachedElementWithPermission(elementId), AXPress, "Invoke");

    public void SetValue(string elementId, string value)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        var cfValue = MacNativeMethods.CreateCFString(value);
        try
        {
            SetAttribute(element, AXValue, cfValue);
        }
        finally
        {
            MacNativeMethods.CFRelease(cfValue);
        }
    }

    public string GetValue(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        return ReadAttributeAsString(element, AXValue)
            ?? ReadAttributeAsString(element, AXTitle)
            ?? "";
    }

    public string ToggleElement(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        PerformRequiredAction(element, AXPress, "Toggle");
        return ReadAttributeAsString(element, AXValue) ?? "Toggled";
    }

    public void ClickAtPoint(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        var point = GetClickablePoint(element);

        lock (s_physicalInputLock)
        {
            PostMouseEvent(MacNativeMethods.KCGEventLeftMouseDown, point);
            PostMouseEvent(MacNativeMethods.KCGEventLeftMouseUp, point);
        }
    }

    public string ExpandElement(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        if (TryPerformAction(element, AXExpand) || TryPerformAction(element, AXShowMenu))
        {
            return "Expanded";
        }

        throw UnsupportedAction(elementId, element, "ExpandCollapse");
    }

    public string CollapseElement(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        if (TryPerformAction(element, AXCollapse))
        {
            return "Collapsed";
        }

        throw UnsupportedAction(elementId, element, "ExpandCollapse");
    }

    public void SelectElement(string elementId)
    {
        EnsureAccessibilityTrusted();
        SetBooleanAttribute(GetCachedElement(elementId), AXSelected, true);
    }

    public void DeselectElement(string elementId)
    {
        EnsureAccessibilityTrusted();
        SetBooleanAttribute(GetCachedElement(elementId), AXSelected, false);
    }

    public SelectionInfo GetSelection(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        var selected = new List<ElementInfo>();

        using var selectedChildren = CopyAttribute(element, AXSelectedChildren);
        if (selectedChildren.Value != IntPtr.Zero && MacNativeMethods.IsType(selectedChildren.Value, MacNativeMethods.CFArrayGetTypeID()))
        {
            foreach (var selectedChild in CopyArrayItems(selectedChildren.Value))
            {
                try
                {
                    selected.Add(ToElementInfo(selectedChild));
                }
                finally
                {
                    MacNativeMethods.CFRelease(selectedChild);
                }
            }
        }
        else
        {
            foreach (var child in GetChildren(element))
            {
                try
                {
                    if (ReadBooleanAttribute(child, AXSelected) == true)
                    {
                        selected.Add(ToElementInfo(child));
                    }
                }
                finally
                {
                    MacNativeMethods.CFRelease(child);
                }
            }
        }

        return new SelectionInfo
        {
            SelectedItems = selected,
            CanSelectMultiple = selected.Count > 1,
            IsSelectionRequired = false,
        };
    }

    public string SetWindowVisualState(string elementId, string state)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);

        switch (state.ToLowerInvariant())
        {
            case "minimized":
                SetBooleanAttribute(element, AXMinimized, true);
                break;
            case "normal":
                TrySetBooleanAttribute(element, AXMinimized, false);
                TrySetBooleanAttribute(element, AXFullScreen, false);
                TryPerformAction(element, AXRaise);
                break;
            case "maximized":
                if (!TrySetBooleanAttribute(element, AXFullScreen, true) && !PressWindowButton(element, AXZoomButton))
                {
                    throw new InvalidOperationException($"Element '{elementId}' does not support maximizing/full-screen.");
                }
                break;
            default:
                throw new ArgumentException($"Invalid window state '{state}'. Valid values: minimized, maximized, normal.");
        }

        return GetWindowInfo(elementId).WindowVisualState;
    }

    public void CloseWindow(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        if (!PressWindowButton(element, AXCloseButton) && !TryPerformAction(element, AXCancel))
        {
            throw UnsupportedAction(elementId, element, "Window");
        }
    }

    public WindowStateInfo GetWindowInfo(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        var role = ReadAttributeAsString(element, AXRole);
        if (!string.Equals(role, "AXWindow", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Element '{elementId}' (Name=\"{GetElementName(element)}\") is not a window.");
        }

        var minimized = ReadBooleanAttribute(element, AXMinimized) == true;
        var fullScreen = ReadBooleanAttribute(element, AXFullScreen) == true;

        return new WindowStateInfo
        {
            WindowVisualState = minimized ? "Minimized" : fullScreen ? "Maximized" : "Normal",
            WindowInteractionState = "ReadyForUserInteraction",
            CanMaximize = IsAttributeSettable(element, AXFullScreen) || HasWindowButton(element, AXZoomButton),
            CanMinimize = IsAttributeSettable(element, AXMinimized) || HasWindowButton(element, AXMinimizeButton),
            IsModal = string.Equals(ReadAttributeAsString(element, AXSubrole), "AXDialog", StringComparison.OrdinalIgnoreCase),
            IsTopmost = false,
        };
    }

    public ScrollInfo Scroll(string elementId, string? horizontalAmount, string? verticalAmount)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        var point = GetClickablePoint(element);
        int horizontal = ToScrollDelta(horizontalAmount, nameof(horizontalAmount));
        int vertical = ToScrollDelta(verticalAmount, nameof(verticalAmount));

        if (horizontal == 0 && vertical == 0)
        {
            return CreateUnknownScrollInfo();
        }

        var scrollEvent = MacNativeMethods.CGEventCreateScrollWheelEvent(
            IntPtr.Zero,
            MacNativeMethods.KCGScrollEventUnitLine,
            2,
            vertical,
            horizontal);
        if (scrollEvent == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to create macOS scroll event.");
        }

        lock (s_physicalInputLock)
        {
            try
            {
                PostMouseEvent(MacNativeMethods.KCGEventLeftMouseDown, point);
                PostMouseEvent(MacNativeMethods.KCGEventLeftMouseUp, point);
                MacNativeMethods.CGEventPost(MacNativeMethods.KCGHIDEventTap, scrollEvent);
            }
            finally
            {
                MacNativeMethods.CFRelease(scrollEvent);
            }
        }

        return CreateUnknownScrollInfo();
    }

    public ScrollInfo SetScrollPercent(string elementId, double? horizontalPercent, double? verticalPercent)
    {
        EnsureAccessibilityTrusted();
        _ = GetCachedElement(elementId);
        throw new InvalidOperationException("macOS Accessibility does not expose a generic absolute scroll-percent API.");
    }

    public void ScrollIntoView(string elementId)
    {
        EnsureAccessibilityTrusted();
        PerformRequiredAction(GetCachedElement(elementId), AXScrollToVisible, "ScrollItem");
    }

    public ElementInfo GetFocusedElement()
    {
        EnsureAccessibilityTrusted();
        using var systemWide = new CFHandle(MacNativeMethods.AXUIElementCreateSystemWide());
        using var focused = CopyAttribute(systemWide.Value, AXFocusedUIElement);
        if (focused.Value == IntPtr.Zero)
        {
            throw new InvalidOperationException("No element currently has keyboard focus.");
        }

        return ToElementInfo(focused.Value);
    }

    public void SetFocus(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        TryPerformAction(element, AXRaise);
        SetBooleanAttribute(element, AXFocused, true);
    }

    public void SendKeys(string elementId, string keys)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);

        lock (s_physicalInputLock)
        {
            TryPerformAction(element, AXRaise);
            TrySetBooleanAttribute(element, AXFocused, true);
            Thread.Sleep(50);
            MacKeyInputHelper.Post(keys);
        }
    }

    public RangeValueInfo GetRangeValue(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        var value = ReadDoubleAttribute(element, AXValue)
            ?? throw new InvalidOperationException($"Element '{elementId}' (Name=\"{GetElementName(element)}\") does not expose a numeric AXValue.");

        return new RangeValueInfo
        {
            Value = value,
            Minimum = ReadDoubleAttribute(element, AXMinValue) ?? 0,
            Maximum = ReadDoubleAttribute(element, AXMaxValue) ?? 100,
            SmallChange = ReadDoubleAttribute(element, AXIncrement) ?? 1,
            LargeChange = ReadDoubleAttribute(element, AXIncrement) ?? 10,
            IsReadOnly = !IsAttributeSettable(element, AXValue),
        };
    }

    public RangeValueInfo SetRangeValue(string elementId, double value)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        if (!IsAttributeSettable(element, AXValue))
        {
            throw new InvalidOperationException($"Element '{elementId}' (Name=\"{GetElementName(element)}\") has a read-only AXValue.");
        }

        var cfValue = MacNativeMethods.CFNumberCreateDouble(IntPtr.Zero, MacNativeMethods.KCFNumberDoubleType, ref value);
        if (cfValue == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to allocate CFNumber for range value.");
        }

        try
        {
            SetAttribute(element, AXValue, cfValue);
        }
        finally
        {
            MacNativeMethods.CFRelease(cfValue);
        }

        return GetRangeValue(elementId);
    }

    public string GetText(string elementId, int maxLength = -1)
    {
        EnsureAccessibilityTrusted();
        var text = GetValue(elementId);
        return maxLength >= 0 && text.Length > maxLength ? text[..maxLength] : text;
    }

    public GridInfo GetGridItem(string elementId, int row, int column)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        var rows = ReadElementArrayAttribute(element, AXRows);
        if (rows.Count == 0)
        {
            throw new InvalidOperationException($"Element '{elementId}' (Name=\"{GetElementName(element)}\") does not expose rows.");
        }

        try
        {
            if (row < 0 || row >= rows.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(row), row, $"Row must be between 0 and {rows.Count - 1}.");
            }

            var cells = GetChildren(rows[row]);
            try
            {
                if (column < 0 || column >= cells.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(column), column, $"Column must be between 0 and {cells.Count - 1}.");
                }

                return new GridInfo
                {
                    Item = ToElementInfo(cells[column]),
                    RowCount = rows.Count,
                    ColumnCount = cells.Count,
                };
            }
            finally
            {
                ReleaseAll(cells);
            }
        }
        finally
        {
            ReleaseAll(rows);
        }
    }

    public TableHeaderInfo GetTableHeaders(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        var role = MapRoleToControlType(ReadAttributeAsString(element, AXRole), ReadAttributeAsString(element, AXSubrole));
        if (!string.Equals(role, "Table", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(role, "DataGrid", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Element '{elementId}' (Name=\"{GetElementName(element)}\") does not expose table headers.");
        }

        var columnHeaders = new List<ElementInfo>();
        var columns = ReadElementArrayAttribute(element, AXColumns);
        try
        {
            foreach (var column in columns)
            {
                columnHeaders.Add(ToElementInfo(column));
            }
        }
        finally
        {
            ReleaseAll(columns);
        }

        return new TableHeaderInfo
        {
            RowHeaders = [],
            ColumnHeaders = columnHeaders,
            RowOrColumnMajor = "RowMajor",
        };
    }

    public void MoveElement(string elementId, double x, double y)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        var position = new MacNativeMethods.CGPoint(x, y);
        var value = MacNativeMethods.AXValueCreate(MacNativeMethods.KAXValueCGPointType, ref position);
        if (value == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to allocate AXValue for element position.");
        }

        try
        {
            SetAttribute(element, AXPosition, value);
        }
        finally
        {
            MacNativeMethods.CFRelease(value);
        }
    }

    public void ResizeElement(string elementId, double width, double height)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        var size = new MacNativeMethods.CGSize(width, height);
        var value = MacNativeMethods.AXValueCreateCGSize(MacNativeMethods.KAXValueCGSizeType, ref size);
        if (value == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to allocate AXValue for element size.");
        }

        try
        {
            SetAttribute(element, AXSize, value);
        }
        finally
        {
            MacNativeMethods.CFRelease(value);
        }
    }

    public ElementInfo? GetParent(string elementId)
    {
        EnsureAccessibilityTrusted();
        var element = GetCachedElement(elementId);
        using var parent = CopyAttribute(element, AXParent);
        if (parent.Value == IntPtr.Zero)
        {
            return null;
        }

        return ToElementInfo(parent.Value);
    }

    private void CollectMatchingFlat(
        IntPtr parent,
        ElementFilter? filter,
        int? maxDepth,
        int maxResults,
        List<ElementInfo> results,
        ref int scannedCount,
        ref int matchedCount)
    {
        if (maxDepth.HasValue && maxDepth.Value <= 0)
        {
            return;
        }

        foreach (var child in GetChildren(parent))
        {
            try
            {
                scannedCount++;
                if (MatchesFilter(child, filter))
                {
                    matchedCount++;
                    if (results.Count < maxResults)
                    {
                        results.Add(ToElementInfo(child));
                    }
                }

                CollectMatchingFlat(child, filter, maxDepth.HasValue ? maxDepth.Value - 1 : null, maxResults, results, ref scannedCount, ref matchedCount);
            }
            catch (ElementStaleException)
            {
            }
            finally
            {
                MacNativeMethods.CFRelease(child);
            }
        }
    }

    private List<ElementInfo> CollectMatchingTree(
        IntPtr parent,
        ElementFilter? filter,
        int? maxDepth,
        int maxResults,
        ref int scannedCount,
        ref int matchedCount)
    {
        var results = new List<ElementInfo>();
        if (maxDepth.HasValue && maxDepth.Value <= 0)
        {
            return results;
        }

        foreach (var child in GetChildren(parent))
        {
            try
            {
                scannedCount++;
                bool thisMatches = MatchesFilter(child, filter);
                if (thisMatches)
                {
                    matchedCount++;
                }

                var childResults = CollectMatchingTree(child, filter, maxDepth.HasValue ? maxDepth.Value - 1 : null, maxResults, ref scannedCount, ref matchedCount);
                if (thisMatches || childResults.Count > 0)
                {
                    var info = ToElementInfo(child);
                    info.Children = childResults.Count > 0 ? childResults : null;
                    results.Add(info);
                }
            }
            catch (ElementStaleException)
            {
            }
            finally
            {
                MacNativeMethods.CFRelease(child);
            }
        }

        return results.Count > maxResults ? results[..maxResults] : results;
    }

    private bool MatchesFilter(IntPtr element, ElementFilter? filter)
    {
        if (filter is null || filter.IsEmpty)
        {
            return true;
        }

        var role = MapRoleToControlType(ReadAttributeAsString(element, AXRole), ReadAttributeAsString(element, AXSubrole));
        var name = GetElementName(element);
        var automationId = ReadAttributeAsString(element, AXIdentifier) ?? "";
        var className = ReadAttributeAsString(element, AXRole) ?? "";
        var enabled = ReadBooleanAttribute(element, AXEnabled) ?? true;
        var isOffscreen = IsOffscreen(element);

        if (filter.IsEnabled.HasValue && enabled != filter.IsEnabled.Value)
        {
            return false;
        }

        if (filter.IsOffscreen.HasValue && isOffscreen != filter.IsOffscreen.Value)
        {
            return false;
        }

        if (filter.ControlTypes is { Length: > 0 } && !filter.ControlTypes.Any(ct => string.Equals(ct, role, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(filter.NameContains) && !name.Contains(filter.NameContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(filter.AutomationIdContains) && !automationId.Contains(filter.AutomationIdContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(filter.ClassNameContains) && !className.Contains(filter.ClassNameContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filter.SupportedPatterns is { Length: > 0 })
        {
            var patterns = GetSupportedPatternNames(element);
            if (!filter.SupportedPatterns.Any(fp => patterns.Any(p => string.Equals(p, fp, StringComparison.OrdinalIgnoreCase))))
            {
                return false;
            }
        }

        return true;
    }

    private List<ElementInfo> GetChildrenRecursive(IntPtr parent, int remainingDepth)
    {
        var children = new List<ElementInfo>();
        if (remainingDepth <= 0)
        {
            return children;
        }

        foreach (var child in GetChildren(parent))
        {
            try
            {
                var info = ToElementInfo(child);
                if (remainingDepth > 1)
                {
                    info.Children = GetChildrenRecursive(child, remainingDepth - 1);
                }

                children.Add(info);
            }
            catch (ElementStaleException)
            {
            }
            finally
            {
                MacNativeMethods.CFRelease(child);
            }
        }

        return children;
    }

    private void CollectDescendants(IntPtr element, Action<IntPtr> visit)
    {
        visit(element);
        foreach (var child in GetChildren(element))
        {
            try
            {
                CollectDescendants(child, visit);
            }
            finally
            {
                MacNativeMethods.CFRelease(child);
            }
        }
    }

    private static bool MatchesSimpleFilter(ElementInfo info, string? name, string? automationId, string? controlType)
    {
        if (!string.IsNullOrEmpty(name) && !string.Equals(info.Name, name, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(automationId) && !string.Equals(info.AutomationId, automationId, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(controlType) && !string.Equals(info.ControlType, controlType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private ElementInfo ToElementInfo(IntPtr element, string? existingId = null)
    {
        int pid = 0;
        _ = MacNativeMethods.AXUIElementGetPid(element, out pid);

        var role = ReadAttributeAsString(element, AXRole);
        var subrole = ReadAttributeAsString(element, AXSubrole);
        var bounds = GetBounds(element);

        return new ElementInfo
        {
            ElementId = existingId ?? _cache.GetOrAdd(element),
            Name = GetElementName(element),
            AutomationId = ReadAttributeAsString(element, AXIdentifier) ?? "",
            ControlType = MapRoleToControlType(role, subrole),
            ClassName = role ?? "",
            LocalizedControlType = role?.Replace("AX", "", StringComparison.Ordinal) ?? "",
            BoundingRectangle = bounds,
            IsEnabled = ReadBooleanAttribute(element, AXEnabled) ?? true,
            IsOffscreen = bounds is null || bounds.Width <= 0 || bounds.Height <= 0,
            ProcessId = pid,
            SupportedPatterns = GetSupportedPatternNames(element),
            HasKeyboardFocus = ReadBooleanAttribute(element, AXFocused) ?? false,
            IsKeyboardFocusable = IsAttributeSettable(element, AXFocused),
            HelpText = NullIfEmpty(ReadAttributeAsString(element, AXDescription)),
            AcceleratorKey = null,
            AccessKey = null,
            NativeWindowHandle = null,
            FrameworkId = "macOS",
        };
    }

    private static string[] GetSupportedPatternNames(IntPtr element)
    {
        var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var actions = GetActionNames(element);

        if (actions.Contains(AXPress))
        {
            patterns.Add("Invoke");
            patterns.Add("Toggle");
        }

        if (actions.Contains(AXExpand) || actions.Contains(AXCollapse) || actions.Contains(AXShowMenu))
        {
            patterns.Add("ExpandCollapse");
        }

        if (actions.Contains(AXScrollToVisible))
        {
            patterns.Add("ScrollItem");
        }

        if (actions.Contains(AXIncrementAction) || actions.Contains(AXDecrementAction))
        {
            patterns.Add("RangeValue");
        }

        using var valueAttribute = CopyAttribute(element, AXValue);
        if (valueAttribute.Value != IntPtr.Zero)
        {
            patterns.Add("Value");
            patterns.Add("Text");
            if (ReadDoubleAttribute(element, AXValue).HasValue)
            {
                patterns.Add("RangeValue");
            }
        }

        if (IsAttributeSettable(element, AXSelected))
        {
            patterns.Add("SelectionItem");
        }

        using var selectedChildrenAttribute = CopyAttribute(element, AXSelectedChildren);
        if (selectedChildrenAttribute.Value != IntPtr.Zero)
        {
            patterns.Add("Selection");
        }

        var role = ReadAttributeAsString(element, AXRole);
        if (string.Equals(role, "AXWindow", StringComparison.OrdinalIgnoreCase))
        {
            patterns.Add("Window");
            patterns.Add("Transform");
        }

        using var rowsAttribute = CopyAttribute(element, AXRows);
        if (rowsAttribute.Value != IntPtr.Zero)
        {
            patterns.Add("Grid");
            patterns.Add("Table");
        }

        return patterns.ToArray();
    }

    private static HashSet<string> GetActionNames(IntPtr element)
    {
        var actions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int error = MacNativeMethods.AXUIElementCopyActionNames(element, out var actionArray);
        if (error != MacNativeMethods.KAXErrorSuccess || actionArray == IntPtr.Zero)
        {
            return actions;
        }

        try
        {
            var count = MacNativeMethods.CFArrayGetCount(actionArray);
            for (nint i = 0; i < count; i++)
            {
                var action = MacNativeMethods.CFArrayGetValueAtIndex(actionArray, i);
                var actionName = MacNativeMethods.CFStringToString(action);
                if (!string.IsNullOrEmpty(actionName))
                {
                    actions.Add(actionName);
                }
            }
        }
        finally
        {
            MacNativeMethods.CFRelease(actionArray);
        }

        return actions;
    }

    private List<IntPtr> GetChildren(IntPtr element) => ReadElementArrayAttribute(element, AXChildren);

    private static List<IntPtr> ReadElementArrayAttribute(IntPtr element, string attribute)
    {
        using var array = CopyAttribute(element, attribute);
        if (array.Value == IntPtr.Zero || !MacNativeMethods.IsType(array.Value, MacNativeMethods.CFArrayGetTypeID()))
        {
            return [];
        }

        return CopyArrayItems(array.Value);
    }

    private static List<IntPtr> CopyArrayItems(IntPtr array)
    {
        var count = MacNativeMethods.CFArrayGetCount(array);
        var items = new List<IntPtr>((int)count);

        for (nint i = 0; i < count; i++)
        {
            var item = MacNativeMethods.CFArrayGetValueAtIndex(array, i);
            if (item != IntPtr.Zero && MacNativeMethods.IsType(item, MacNativeMethods.AXUIElementGetTypeID()))
            {
                items.Add(MacNativeMethods.CFRetain(item));
            }
        }

        return items;
    }

    private static void ReleaseAll(IEnumerable<IntPtr> values)
    {
        foreach (var value in values)
        {
            MacNativeMethods.CFRelease(value);
        }
    }

    private static BoundsInfo? GetBounds(IntPtr element)
    {
        var position = ReadPointAttribute(element, AXPosition);
        var size = ReadSizeAttribute(element, AXSize);
        if (position is null || size is null)
        {
            return null;
        }

        return new BoundsInfo
        {
            X = SanitizeDouble(position.Value.X),
            Y = SanitizeDouble(position.Value.Y),
            Width = SanitizeDouble(size.Value.Width),
            Height = SanitizeDouble(size.Value.Height),
        };
    }

    private static MacNativeMethods.CGPoint? ReadPointAttribute(IntPtr element, string attribute)
    {
        using var value = CopyAttribute(element, attribute);
        if (value.Value == IntPtr.Zero || !MacNativeMethods.IsType(value.Value, MacNativeMethods.AXValueGetTypeID()))
        {
            return null;
        }

        return MacNativeMethods.AXValueGetCGPointValue(value.Value, MacNativeMethods.KAXValueCGPointType, out var point)
            ? point
            : null;
    }

    private static MacNativeMethods.CGSize? ReadSizeAttribute(IntPtr element, string attribute)
    {
        using var value = CopyAttribute(element, attribute);
        if (value.Value == IntPtr.Zero || !MacNativeMethods.IsType(value.Value, MacNativeMethods.AXValueGetTypeID()))
        {
            return null;
        }

        return MacNativeMethods.AXValueGetCGSizeValue(value.Value, MacNativeMethods.KAXValueCGSizeType, out var size)
            ? size
            : null;
    }

    private static string GetElementName(IntPtr element) =>
        ReadAttributeAsString(element, AXTitle)
        ?? ReadAttributeAsString(element, AXDescription)
        ?? ReadAttributeAsString(element, AXValue)
        ?? "";

    private static string? ReadAttributeAsString(IntPtr element, string attribute)
    {
        using var value = CopyAttribute(element, attribute);
        return CFValueToString(value.Value);
    }

    private static string? CFValueToString(IntPtr value)
    {
        if (value == IntPtr.Zero)
        {
            return null;
        }

        if (MacNativeMethods.IsType(value, MacNativeMethods.CFStringGetTypeID()))
        {
            return MacNativeMethods.CFStringToString(value);
        }

        if (MacNativeMethods.IsType(value, MacNativeMethods.CFBooleanGetTypeID()))
        {
            return MacNativeMethods.CFBooleanGetValue(value).ToString();
        }

        if (MacNativeMethods.IsType(value, MacNativeMethods.CFNumberGetTypeID()))
        {
            if (MacNativeMethods.CFNumberGetDoubleValue(value, MacNativeMethods.KCFNumberDoubleType, out var doubleValue))
            {
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private static bool? ReadBooleanAttribute(IntPtr element, string attribute)
    {
        using var value = CopyAttribute(element, attribute);
        if (value.Value == IntPtr.Zero || !MacNativeMethods.IsType(value.Value, MacNativeMethods.CFBooleanGetTypeID()))
        {
            return null;
        }

        return MacNativeMethods.CFBooleanGetValue(value.Value);
    }

    private static double? ReadDoubleAttribute(IntPtr element, string attribute)
    {
        using var value = CopyAttribute(element, attribute);
        if (value.Value == IntPtr.Zero || !MacNativeMethods.IsType(value.Value, MacNativeMethods.CFNumberGetTypeID()))
        {
            return null;
        }

        return MacNativeMethods.CFNumberGetDoubleValue(value.Value, MacNativeMethods.KCFNumberDoubleType, out var doubleValue)
            ? doubleValue
            : null;
    }

    private static void SetBooleanAttribute(IntPtr element, string attribute, bool value)
    {
        if (!TrySetBooleanAttribute(element, attribute, value))
        {
            throw new InvalidOperationException($"Attribute '{attribute}' is not settable for element '{GetElementName(element)}'.");
        }
    }

    private static bool TrySetBooleanAttribute(IntPtr element, string attribute, bool value)
    {
        var cfValue = value ? MacNativeMethods.KCFBooleanTrue : MacNativeMethods.KCFBooleanFalse;
        using var cfAttribute = new CFHandle(MacNativeMethods.CreateCFString(attribute));
        int error = MacNativeMethods.AXUIElementSetAttributeValue(element, cfAttribute.Value, cfValue);
        return error == MacNativeMethods.KAXErrorSuccess;
    }

    private static void SetAttribute(IntPtr element, string attribute, IntPtr value)
    {
        using var cfAttribute = new CFHandle(MacNativeMethods.CreateCFString(attribute));
        int error = MacNativeMethods.AXUIElementSetAttributeValue(element, cfAttribute.Value, value);
        ThrowIfAxError(error, $"set attribute '{attribute}'");
    }

    private static bool IsAttributeSettable(IntPtr element, string attribute)
    {
        using var cfAttribute = new CFHandle(MacNativeMethods.CreateCFString(attribute));
        int error = MacNativeMethods.AXUIElementIsAttributeSettable(element, cfAttribute.Value, out bool settable);
        return error == MacNativeMethods.KAXErrorSuccess && settable;
    }

    private static CFHandle CopyAttribute(IntPtr element, string attribute)
    {
        using var cfAttribute = new CFHandle(MacNativeMethods.CreateCFString(attribute));
        int error = MacNativeMethods.AXUIElementCopyAttributeValue(element, cfAttribute.Value, out var value);

        if (error is MacNativeMethods.KAXErrorAttributeUnsupported
            or MacNativeMethods.KAXErrorNoValue
            or MacNativeMethods.KAXErrorCannotComplete
            or MacNativeMethods.KAXErrorFailure)
        {
            return new CFHandle(IntPtr.Zero);
        }

        ThrowIfAxError(error, $"copy attribute '{attribute}'");
        return new CFHandle(value);
    }

    private static void PerformRequiredAction(IntPtr element, string action, string patternName)
    {
        if (!TryPerformAction(element, action))
        {
            throw new InvalidOperationException($"Element '{GetElementName(element)}' does not support {patternName}Pattern.");
        }
    }

    private static bool TryPerformAction(IntPtr element, string action)
    {
        using var cfAction = new CFHandle(MacNativeMethods.CreateCFString(action));
        int error = MacNativeMethods.AXUIElementPerformAction(element, cfAction.Value);
        return error == MacNativeMethods.KAXErrorSuccess;
    }

    private static bool PressWindowButton(IntPtr window, string buttonAttribute)
    {
        using var button = CopyAttribute(window, buttonAttribute);
        return button.Value != IntPtr.Zero && TryPerformAction(button.Value, AXPress);
    }

    private static bool HasWindowButton(IntPtr window, string buttonAttribute)
    {
        using var button = CopyAttribute(window, buttonAttribute);
        return button.Value != IntPtr.Zero;
    }

    private static InvalidOperationException UnsupportedAction(string elementId, IntPtr element, string patternName) =>
        new($"Element '{elementId}' (Name=\"{GetElementName(element)}\") does not support {patternName}Pattern.");

    private static MacNativeMethods.CGPoint GetClickablePoint(IntPtr element)
    {
        var bounds = GetBounds(element)
            ?? throw new InvalidOperationException($"Element '{GetElementName(element)}' has no accessible bounds.");

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException($"Element '{GetElementName(element)}' has an empty bounding rectangle.");
        }

        return new MacNativeMethods.CGPoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    private static void PostMouseEvent(uint eventType, MacNativeMethods.CGPoint point)
    {
        var mouseEvent = MacNativeMethods.CGEventCreateMouseEvent(IntPtr.Zero, eventType, point, MacNativeMethods.KCGMouseButtonLeft);
        if (mouseEvent == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to create macOS mouse event.");
        }

        try
        {
            MacNativeMethods.CGEventPost(MacNativeMethods.KCGHIDEventTap, mouseEvent);
        }
        finally
        {
            MacNativeMethods.CFRelease(mouseEvent);
        }
    }

    private static ScrollInfo CreateUnknownScrollInfo() => new()
    {
        HorizontalScrollPercent = 0,
        VerticalScrollPercent = 0,
        HorizontalViewSize = 0,
        VerticalViewSize = 0,
        HorizontallyScrollable = true,
        VerticallyScrollable = true,
    };

    private static int ToScrollDelta(string? amount, string paramName)
    {
        if (string.IsNullOrEmpty(amount) || string.Equals(amount, "NoAmount", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return amount.ToLowerInvariant() switch
        {
            "smallincrement" => -3,
            "largeincrement" => -10,
            "smalldecrement" => 3,
            "largedecrement" => 10,
            _ => throw new ArgumentException(
                $"Invalid scroll amount '{amount}' for {paramName}. Valid values: SmallIncrement, LargeIncrement, SmallDecrement, LargeDecrement, NoAmount."),
        };
    }

    private IntPtr GetCachedElementWithPermission(string elementId)
    {
        EnsureAccessibilityTrusted();
        return GetCachedElement(elementId);
    }

    private IntPtr GetCachedElement(string elementId)
    {
        if (!_cache.TryGet(elementId, out var element))
        {
            throw new KeyNotFoundException($"No cached element found with ID '{elementId}'. Use list_windows or find_element first.");
        }

        return element;
    }

    private static void EnsureAccessibilityTrusted()
    {
        if (!IsAccessibilityTrusted(prompt: true))
        {
            throw new InvalidOperationException(
                "macOS Accessibility permission is required. Add the MCP executable to System Settings > Privacy & Security > Accessibility, then restart the MCP client. " +
                $"Executable: {GetExecutablePath()}");
        }
    }

    private static bool IsAccessibilityTrusted(bool prompt)
    {
        if (!prompt)
        {
            return MacNativeMethods.AXIsProcessTrusted();
        }

        var keys = new[] { MacNativeMethods.KAXTrustedCheckOptionPrompt };
        var values = new[] { MacNativeMethods.KCFBooleanTrue };
        var options = MacNativeMethods.CFDictionaryCreate(IntPtr.Zero, keys, values, 1, IntPtr.Zero, IntPtr.Zero);
        if (options == IntPtr.Zero)
        {
            return MacNativeMethods.AXIsProcessTrusted();
        }

        try
        {
            return MacNativeMethods.AXIsProcessTrustedWithOptions(options);
        }
        finally
        {
            MacNativeMethods.CFRelease(options);
        }
    }

    private static string GetExecutablePath() =>
        Environment.ProcessPath ?? AppContext.BaseDirectory;

    private static IEnumerable<int> GetOnScreenWindowProcessIds()
    {
        var pids = new HashSet<int>();
        var windowInfo = MacNativeMethods.CGWindowListCopyWindowInfo(
            MacNativeMethods.KCGWindowListOptionOnScreenOnly | MacNativeMethods.KCGWindowListExcludeDesktopElements,
            MacNativeMethods.KCGNullWindowID);

        if (windowInfo == IntPtr.Zero)
        {
            return pids;
        }

        using var ownerPidKey = new CFHandle(MacNativeMethods.CreateCFString("kCGWindowOwnerPID"));

        try
        {
            var count = MacNativeMethods.CFArrayGetCount(windowInfo);
            for (nint i = 0; i < count; i++)
            {
                var dictionary = MacNativeMethods.CFArrayGetValueAtIndex(windowInfo, i);
                var value = MacNativeMethods.CFDictionaryGetValue(dictionary, ownerPidKey.Value);
                if (value != IntPtr.Zero
                    && MacNativeMethods.CFNumberGetValue(value, MacNativeMethods.KCFNumberIntType, out var pid)
                    && pid > 0)
                {
                    pids.Add(pid);
                }
            }
        }
        finally
        {
            MacNativeMethods.CFRelease(windowInfo);
        }

        return pids;
    }

    private static bool IsOffscreen(IntPtr element)
    {
        var bounds = GetBounds(element);
        return bounds is null || bounds.Width <= 0 || bounds.Height <= 0;
    }

    private static string MapRoleToControlType(string? role, string? subrole)
    {
        if (string.Equals(subrole, "AXDialog", StringComparison.OrdinalIgnoreCase))
        {
            return "Window";
        }

        return role switch
        {
            "AXWindow" => "Window",
            "AXButton" => "Button",
            "AXCheckBox" => "CheckBox",
            "AXRadioButton" => "RadioButton",
            "AXTextField" => "Edit",
            "AXTextArea" => "Document",
            "AXStaticText" => "Text",
            "AXImage" => "Image",
            "AXMenu" => "Menu",
            "AXMenuBar" => "MenuBar",
            "AXMenuItem" => "MenuItem",
            "AXPopUpButton" => "ComboBox",
            "AXComboBox" => "ComboBox",
            "AXList" => "List",
            "AXRow" => "DataItem",
            "AXCell" => "DataItem",
            "AXTable" => "Table",
            "AXOutline" => "Tree",
            "AXBrowser" => "Tree",
            "AXGroup" => "Group",
            "AXRadioGroup" => "Group",
            "AXScrollArea" => "Pane",
            "AXScrollBar" => "ScrollBar",
            "AXSlider" => "Slider",
            "AXIncrementor" => "Spinner",
            "AXProgressIndicator" => "ProgressBar",
            "AXTabGroup" => "Tab",
            "AXSplitterGroup" => "Pane",
            "AXToolbar" => "ToolBar",
            "AXSheet" => "Window",
            "AXDrawer" => "Pane",
            "AXLink" => "Hyperlink",
            _ => "Custom",
        };
    }

    private static double SanitizeDouble(double value) =>
        double.IsInfinity(value) || double.IsNaN(value) ? 0 : value;

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;

    private static void ThrowIfAxError(int error, string operation)
    {
        switch (error)
        {
            case MacNativeMethods.KAXErrorSuccess:
                return;
            case MacNativeMethods.KAXErrorInvalidUIElement:
            case MacNativeMethods.KAXErrorInvalidUIElementObserver:
                throw new ElementStaleException($"The cached UI element became unavailable while trying to {operation}.");
            case MacNativeMethods.KAXErrorAPIDisabled:
                throw new InvalidOperationException("macOS Accessibility API is disabled or this process is not trusted.");
            case MacNativeMethods.KAXErrorAttributeUnsupported:
            case MacNativeMethods.KAXErrorActionUnsupported:
            case MacNativeMethods.KAXErrorNoValue:
                throw new InvalidOperationException($"macOS Accessibility does not support {operation} for this element.");
            default:
                throw new InvalidOperationException($"macOS Accessibility failed to {operation} (AXError {error}).");
        }
    }

    private readonly struct CFHandle : IDisposable
    {
        public CFHandle(IntPtr value)
        {
            Value = value;
        }

        public IntPtr Value { get; }

        public void Dispose()
        {
            if (Value != IntPtr.Zero)
            {
                MacNativeMethods.CFRelease(Value);
            }
        }
    }
}
#endif
