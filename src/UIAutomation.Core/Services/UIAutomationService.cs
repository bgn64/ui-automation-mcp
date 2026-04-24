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

    public string ExpandElement(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pattern))
        {
            var expandCollapse = (ExpandCollapsePattern)pattern;
            expandCollapse.Expand();
            return expandCollapse.Current.ExpandCollapseState.ToString();
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support ExpandCollapsePattern.");
    });

    public string CollapseElement(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pattern))
        {
            var expandCollapse = (ExpandCollapsePattern)pattern;
            expandCollapse.Collapse();
            return expandCollapse.Current.ExpandCollapseState.ToString();
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support ExpandCollapsePattern.");
    });

    public void SelectElement(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern))
        {
            ((SelectionItemPattern)pattern).Select();
            return;
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support SelectionItemPattern.");
    });

    public void DeselectElement(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern))
        {
            ((SelectionItemPattern)pattern).RemoveFromSelection();
            return;
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support SelectionItemPattern.");
    });

    public SelectionInfo GetSelection(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(SelectionPattern.Pattern, out var pattern))
        {
            var selection = (SelectionPattern)pattern;
            var selectedElements = selection.Current.GetSelection();
            var items = new List<ElementInfo>();

            foreach (AutomationElement selected in selectedElements)
            {
                try { items.Add(ToElementInfo(selected)); }
                catch (ElementNotAvailableException) { }
            }

            return new SelectionInfo
            {
                SelectedItems = items,
                CanSelectMultiple = selection.Current.CanSelectMultiple,
                IsSelectionRequired = selection.Current.IsSelectionRequired,
            };
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support SelectionPattern.");
    });

    public string SetWindowVisualState(string elementId, string state) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (!element.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern))
        {
            throw new InvalidOperationException(
                $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support WindowPattern.");
        }

        var windowPattern = (WindowPattern)pattern;
        var visualState = state.ToLowerInvariant() switch
        {
            "minimized" => WindowVisualState.Minimized,
            "maximized" => WindowVisualState.Maximized,
            "normal" => WindowVisualState.Normal,
            _ => throw new ArgumentException(
                $"Invalid window state '{state}'. Valid values: minimized, maximized, normal."),
        };

        windowPattern.SetWindowVisualState(visualState);
        return windowPattern.Current.WindowVisualState.ToString();
    });

    public void CloseWindow(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern))
        {
            ((WindowPattern)pattern).Close();
            return;
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support WindowPattern.");
    });

    public WindowInfo GetWindowInfo(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern))
        {
            var windowPattern = (WindowPattern)pattern;
            return new WindowInfo
            {
                WindowVisualState = windowPattern.Current.WindowVisualState.ToString(),
                WindowInteractionState = windowPattern.Current.WindowInteractionState.ToString(),
                CanMaximize = windowPattern.Current.CanMaximize,
                CanMinimize = windowPattern.Current.CanMinimize,
                IsModal = windowPattern.Current.IsModal,
                IsTopmost = windowPattern.Current.IsTopmost,
            };
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support WindowPattern.");
    });

    public ScrollInfo Scroll(string elementId, string? horizontalAmount, string? verticalAmount) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (!element.TryGetCurrentPattern(ScrollPattern.Pattern, out var pattern))
        {
            throw new InvalidOperationException(
                $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support ScrollPattern.");
        }

        var scrollPattern = (ScrollPattern)pattern;

        var hAmount = ParseScrollAmount(horizontalAmount, "horizontalAmount");
        var vAmount = ParseScrollAmount(verticalAmount, "verticalAmount");

        if (hAmount != ScrollAmount.NoAmount)
            scrollPattern.ScrollHorizontal(hAmount);
        if (vAmount != ScrollAmount.NoAmount)
            scrollPattern.ScrollVertical(vAmount);

        return ToScrollInfo(scrollPattern);
    });

    public ScrollInfo SetScrollPercent(string elementId, double? horizontalPercent, double? verticalPercent) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (!element.TryGetCurrentPattern(ScrollPattern.Pattern, out var pattern))
        {
            throw new InvalidOperationException(
                $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support ScrollPattern.");
        }

        var scrollPattern = (ScrollPattern)pattern;

        // -1 is the UIA sentinel for "no scroll" (don't change this axis)
        const double NoScroll = -1;
        scrollPattern.SetScrollPercent(
            horizontalPercent ?? NoScroll,
            verticalPercent ?? NoScroll);

        return ToScrollInfo(scrollPattern);
    });

    public void ScrollIntoView(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(ScrollItemPattern.Pattern, out var pattern))
        {
            ((ScrollItemPattern)pattern).ScrollIntoView();
            return;
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support ScrollItemPattern.");
    });

    public ElementInfo GetFocusedElement() => RunOnSta(() =>
    {
        var focused = AutomationElement.FocusedElement;
        if (focused == null)
            throw new InvalidOperationException("No element currently has keyboard focus.");

        return ToElementInfo(focused);
    });

    public void SetFocus(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);
        element.SetFocus();
    });

    public void SendKeys(string elementId, string keys) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        // Serialize all physical input to prevent focus/cursor races.
        lock (PhysicalInputLock)
        {
            element.SetFocus();
            Thread.Sleep(50); // Brief pause to let focus settle

            var inputs = KeyInputParser.Parse(keys);
            if (inputs.Length == 0)
                return;

            uint sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
            if (sent != inputs.Length)
            {
                throw new InvalidOperationException(
                    $"SendInput failed: sent {sent} of {inputs.Length} keyboard events (error {Marshal.GetLastWin32Error()}).");
            }
        }
    });

    public RangeValueInfo GetRangeValue(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(RangeValuePattern.Pattern, out var pattern))
        {
            var rv = (RangeValuePattern)pattern;
            return new RangeValueInfo
            {
                Value = rv.Current.Value,
                Minimum = rv.Current.Minimum,
                Maximum = rv.Current.Maximum,
                SmallChange = rv.Current.SmallChange,
                LargeChange = rv.Current.LargeChange,
                IsReadOnly = rv.Current.IsReadOnly,
            };
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support RangeValuePattern.");
    });

    public RangeValueInfo SetRangeValue(string elementId, double value) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(RangeValuePattern.Pattern, out var pattern))
        {
            var rv = (RangeValuePattern)pattern;

            if (rv.Current.IsReadOnly)
            {
                throw new InvalidOperationException(
                    $"Element '{elementId}' (Name=\"{element.Current.Name}\") has a read-only RangeValuePattern.");
            }

            rv.SetValue(value);
            return new RangeValueInfo
            {
                Value = rv.Current.Value,
                Minimum = rv.Current.Minimum,
                Maximum = rv.Current.Maximum,
                SmallChange = rv.Current.SmallChange,
                LargeChange = rv.Current.LargeChange,
                IsReadOnly = rv.Current.IsReadOnly,
            };
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support RangeValuePattern.");
    });

    public string GetText(string elementId, int maxLength = -1) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
        {
            var textPattern = (TextPattern)pattern;
            return textPattern.DocumentRange.GetText(maxLength);
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support TextPattern.");
    });

    public GridInfo GetGridItem(string elementId, int row, int column) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(GridPattern.Pattern, out var pattern))
        {
            var grid = (GridPattern)pattern;
            var item = grid.GetItem(row, column);
            return new GridInfo
            {
                Item = ToElementInfo(item),
                RowCount = grid.Current.RowCount,
                ColumnCount = grid.Current.ColumnCount,
            };
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support GridPattern.");
    });

    public TableHeaderInfo GetTableHeaders(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (element.TryGetCurrentPattern(TablePattern.Pattern, out var pattern))
        {
            var table = (TablePattern)pattern;
            var rowHeaders = new List<ElementInfo>();
            var columnHeaders = new List<ElementInfo>();

            foreach (AutomationElement header in table.Current.GetRowHeaders())
            {
                try { rowHeaders.Add(ToElementInfo(header)); }
                catch (ElementNotAvailableException) { }
            }

            foreach (AutomationElement header in table.Current.GetColumnHeaders())
            {
                try { columnHeaders.Add(ToElementInfo(header)); }
                catch (ElementNotAvailableException) { }
            }

            return new TableHeaderInfo
            {
                RowHeaders = rowHeaders,
                ColumnHeaders = columnHeaders,
                RowOrColumnMajor = table.Current.RowOrColumnMajor.ToString(),
            };
        }

        throw new InvalidOperationException(
            $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support TablePattern.");
    });

    public void MoveElement(string elementId, double x, double y) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (!element.TryGetCurrentPattern(TransformPattern.Pattern, out var pattern))
        {
            throw new InvalidOperationException(
                $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support TransformPattern.");
        }

        var transform = (TransformPattern)pattern;
        if (!transform.Current.CanMove)
        {
            throw new InvalidOperationException(
                $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support moving (CanMove is false).");
        }

        transform.Move(x, y);
    });

    public void ResizeElement(string elementId, double width, double height) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);

        if (!element.TryGetCurrentPattern(TransformPattern.Pattern, out var pattern))
        {
            throw new InvalidOperationException(
                $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support TransformPattern.");
        }

        var transform = (TransformPattern)pattern;
        if (!transform.Current.CanResize)
        {
            throw new InvalidOperationException(
                $"Element '{elementId}' (Name=\"{element.Current.Name}\") does not support resizing (CanResize is false).");
        }

        transform.Resize(width, height);
    });

    public ElementInfo? GetParent(string elementId) => RunOnSta(() =>
    {
        var element = GetCachedElement(elementId);
        var parent = TreeWalker.ControlViewWalker.GetParent(element);

        if (parent == null || parent == AutomationElement.RootElement)
            return null;

        return ToElementInfo(parent);
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
            HasKeyboardFocus = current.HasKeyboardFocus,
            IsKeyboardFocusable = current.IsKeyboardFocusable,
            HelpText = NullIfEmpty(current.HelpText),
            AcceleratorKey = NullIfEmpty(current.AcceleratorKey),
            AccessKey = NullIfEmpty(current.AccessKey),
            NativeWindowHandle = current.NativeWindowHandle != 0 ? current.NativeWindowHandle : null,
            FrameworkId = NullIfEmpty(current.FrameworkId),
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

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrEmpty(s) ? null : s;

    private static ScrollAmount ParseScrollAmount(string? amount, string paramName)
    {
        if (string.IsNullOrEmpty(amount))
            return ScrollAmount.NoAmount;

        return amount.ToLowerInvariant() switch
        {
            "smallincrement" => ScrollAmount.SmallIncrement,
            "largeincrement" => ScrollAmount.LargeIncrement,
            "smalldecrement" => ScrollAmount.SmallDecrement,
            "largedecrement" => ScrollAmount.LargeDecrement,
            "noamount" => ScrollAmount.NoAmount,
            _ => throw new ArgumentException(
                $"Invalid scroll amount '{amount}' for {paramName}. " +
                "Valid values: SmallIncrement, LargeIncrement, SmallDecrement, LargeDecrement, NoAmount."),
        };
    }

    private static ScrollInfo ToScrollInfo(ScrollPattern scrollPattern)
    {
        return new ScrollInfo
        {
            HorizontalScrollPercent = SanitizeDouble(scrollPattern.Current.HorizontalScrollPercent),
            VerticalScrollPercent = SanitizeDouble(scrollPattern.Current.VerticalScrollPercent),
            HorizontalViewSize = SanitizeDouble(scrollPattern.Current.HorizontalViewSize),
            VerticalViewSize = SanitizeDouble(scrollPattern.Current.VerticalViewSize),
            HorizontallyScrollable = scrollPattern.Current.HorizontallyScrollable,
            VerticallyScrollable = scrollPattern.Current.VerticallyScrollable,
        };
    }

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
}
