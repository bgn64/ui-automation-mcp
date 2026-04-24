using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UIAutomation.Core.Models;
using UIAutomation.Core.Services;

namespace UIAutomation.Mcp.Tools;

[McpServerToolType]
public sealed class QueryTools
{
    private readonly IUIAutomationService _service;
    private readonly ILogger<QueryTools> _logger;

    public QueryTools(IUIAutomationService service, ILogger<QueryTools> logger)
    {
        _service = service;
        _logger = logger;
    }

    [McpServerTool(Name = "query_elements"), Description(
        "Powerful, flexible search for UI elements under a root element. " +
        "Combines the capabilities of find_element and get_element_tree into a single queryable tool " +
        "with filtering, depth control, flatten/tree output, property projection, and result limiting.\n\n" +
        "Common usage patterns:\n" +
        "- Find all interactive elements: set supportedPatterns to \"Invoke,Toggle,Value,ExpandCollapse,SelectionItem\"\n" +
        "- Find all buttons: set controlTypes to \"Button\"\n" +
        "- Compact discovery: set properties to \"elementId,name,automationId,controlType\" to reduce output size\n" +
        "- Deep search with limit: leave maxDepth unset, rely on maxResults (default 200) to cap output\n\n" +
        "Use this tool instead of multiple get_element_tree calls when you need to search a full subtree " +
        "or want only interactive/filtered elements. Use get_element_tree when you need to understand " +
        "the hierarchical layout structure.")]
    public string QueryElements(
        [Description("The elementId of the root element to search under (from list_windows or a previous find/query)")]
        string rootElementId,

        [Description(
            "Comma-separated control types to include (OR logic). " +
            "Example: \"Button,Edit,CheckBox\". " +
            "Valid types: Button, Calendar, CheckBox, ComboBox, Custom, DataGrid, DataItem, Document, Edit, " +
            "Group, Header, HeaderItem, Hyperlink, Image, List, ListItem, Menu, MenuBar, MenuItem, Pane, " +
            "ProgressBar, RadioButton, ScrollBar, Separator, Slider, Spinner, SplitButton, StatusBar, " +
            "Tab, TabItem, Table, Text, Thumb, TitleBar, ToolBar, ToolTip, Tree, TreeItem, Window. " +
            "Omit to include all control types.")]
        string? controlTypes = null,

        [Description(
            "Comma-separated automation pattern names to filter by (OR logic). " +
            "Only elements supporting at least one of these patterns are returned. " +
            "Example: \"Invoke,Toggle,Value\". " +
            "Common patterns: Invoke (clickable), Toggle (checkboxes), Value (text input), " +
            "ExpandCollapse (dropdowns), SelectionItem (list items), RangeValue (sliders), " +
            "Scroll, Text, ScrollItem. " +
            "Omit to include elements regardless of supported patterns.")]
        string? supportedPatterns = null,

        [Description("Substring filter on element Name (case-insensitive). Only elements whose name contains this string are returned.")]
        string? nameContains = null,

        [Description("Substring filter on element AutomationId (case-insensitive). Only elements whose automation ID contains this string are returned.")]
        string? automationIdContains = null,

        [Description("Substring filter on element ClassName (case-insensitive).")]
        string? classNameContains = null,

        [Description("Filter by enabled state. True = only enabled elements, false = only disabled elements. Omit to include both.")]
        bool? isEnabled = null,

        [Description("Filter by offscreen state. True = only offscreen elements, false = only visible elements. Omit to include both.")]
        bool? isOffscreen = null,

        [Description("Maximum depth to search. 1 = direct children only, 2 = children and grandchildren, etc. Omit for unlimited depth (searches entire subtree).")]
        int? maxDepth = null,

        [Description(
            "Output shape. True (default) returns a flat list of matching elements — best for finding specific elements to interact with. " +
            "False returns a tree preserving parent-child relationships — ancestor elements are included for context even if they don't match the filter.")]
        bool flatten = true,

        [Description("Maximum number of matching elements to return. Default is 200. Increase for exhaustive searches, decrease for faster responses.")]
        int maxResults = 200,

        [Description(
            "Comma-separated list of element properties to include in each result. " +
            "Reduces output size by omitting unnecessary fields. " +
            "Available: elementId, name, automationId, controlType, className, localizedControlType, " +
            "boundingRectangle, isEnabled, isOffscreen, processId, supportedPatterns. " +
            "Note: elementId is always included. When flatten is false, children is always included. " +
            "Omit to return all properties.")]
        string? properties = null)
    {
        try
        {
            var filter = new ElementFilter
            {
                ControlTypes = ParseCommaSeparated(controlTypes),
                SupportedPatterns = ParseCommaSeparated(supportedPatterns),
                NameContains = nameContains,
                AutomationIdContains = automationIdContains,
                ClassNameContains = classNameContains,
                IsEnabled = isEnabled,
                IsOffscreen = isOffscreen,
            };

            var options = new ElementQueryOptions
            {
                Filter = filter,
                MaxDepth = maxDepth,
                Flatten = flatten,
                MaxResults = maxResults,
            };

            var result = _service.QueryElements(rootElementId, options);

            var requestedProps = ParseCommaSeparated(properties);

            if (requestedProps is { Length: > 0 })
            {
                var projected = result.Elements.Select(e => ProjectElement(e, requestedProps, flatten)).ToList();
                return ToolResponse.Success(new
                {
                    elements = projected,
                    result.MatchedCount,
                    result.ScannedCount,
                    result.Truncated,
                });
            }

            return ToolResponse.Success(result);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(QueryElements));
        }
    }

    /// <summary>Parses a comma-separated string into a trimmed array, or null if empty.</summary>
    private static string[]? ParseCommaSeparated(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts : null;
    }

    /// <summary>
    /// Projects an ElementInfo to a dictionary containing only the requested properties.
    /// elementId is always included. children is included in tree mode.
    /// </summary>
    private static Dictionary<string, object?> ProjectElement(ElementInfo element, string[] requestedProps, bool flatten)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["elementId"] = element.ElementId,
        };

        foreach (var prop in requestedProps)
        {
            var key = prop.Trim().ToLowerInvariant();

            // elementId always included above
            if (key == "elementid") continue;

            switch (key)
            {
                case "name":
                    dict["name"] = element.Name;
                    break;
                case "automationid":
                    dict["automationId"] = element.AutomationId;
                    break;
                case "controltype":
                    dict["controlType"] = element.ControlType;
                    break;
                case "classname":
                    dict["className"] = element.ClassName;
                    break;
                case "localizedcontroltype":
                    dict["localizedControlType"] = element.LocalizedControlType;
                    break;
                case "boundingrectangle":
                    dict["boundingRectangle"] = element.BoundingRectangle;
                    break;
                case "isenabled":
                    dict["isEnabled"] = element.IsEnabled;
                    break;
                case "isoffscreen":
                    dict["isOffscreen"] = element.IsOffscreen;
                    break;
                case "processid":
                    dict["processId"] = element.ProcessId;
                    break;
                case "supportedpatterns":
                    dict["supportedPatterns"] = element.SupportedPatterns;
                    break;
            }
        }

        // In tree mode, always include children so the tree structure is preserved
        if (!flatten && element.Children is { Count: > 0 })
        {
            dict["children"] = element.Children.Select(c => ProjectElement(c, requestedProps, flatten)).ToList();
        }

        return dict;
    }
}
