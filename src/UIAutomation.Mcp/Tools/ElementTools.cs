using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UIAutomation.Core.Services;

namespace UIAutomation.Mcp.Tools;

[McpServerToolType]
public sealed class ElementTools
{
    private readonly IUIAutomationService _service;
    private readonly ILogger<ElementTools> _logger;

    public ElementTools(IUIAutomationService service, ILogger<ElementTools> logger)
    {
        _service = service;
        _logger = logger;
    }

    [McpServerTool(Name = "find_element"), Description(
        "Finds UI elements within a parent element matching the given criteria. " +
        "At least one of name, automationId, or controlType should be provided. " +
        "Returns matching elements with their elementIds for use with other tools.")]
    public string FindElement(
        [Description("The elementId of the parent element to search within (from list_windows or a previous find)")]
        string parentElementId,
        [Description("Filter by the element's display name (exact match)")]
        string? name = null,
        [Description("Filter by the element's automation ID (exact match)")]
        string? automationId = null,
        [Description("Filter by control type: Button, CheckBox, ComboBox, Edit, Text, Window, ListItem, MenuItem, etc.")]
        string? controlType = null)
    {
        try
        {
            var elements = _service.FindElements(parentElementId, name, automationId, controlType);
            return ToolResponse.Success(elements);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(FindElement));
        }
    }

    [McpServerTool(Name = "get_element_tree"), Description(
        "Gets the UI element subtree under a given element, useful for exploring " +
        "what controls are available. Returns a tree of elements with their properties. " +
        "Use a small maxDepth (1-3) to avoid overwhelming output.")]
    public string GetElementTree(
        [Description("The elementId of the element whose subtree to explore")]
        string elementId,
        [Description("Maximum depth of the tree to return (default 3, recommended 1-3)")]
        int maxDepth = 3)
    {
        try
        {
            var tree = _service.GetElementTree(elementId, maxDepth);
            return ToolResponse.Success(tree);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetElementTree));
        }
    }

    [McpServerTool(Name = "get_element_info"), Description(
        "Gets detailed properties of a specific cached UI element, including its name, " +
        "control type, automation ID, bounding rectangle, and supported interaction patterns.")]
    public string GetElementInfo(
        [Description("The elementId of the element to inspect")]
        string elementId)
    {
        try
        {
            var info = _service.GetElementInfo(elementId);
            if (info == null)
                return ToolResponse.Error($"Element '{elementId}' not found in cache.");

            return ToolResponse.Success(info);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetElementInfo));
        }
    }

    [McpServerTool(Name = "get_grid_item"), Description(
        "Gets the UI element at a specific row and column in a grid or table. " +
        "The container must support the Grid pattern. Also returns the grid's total row and column counts. " +
        "Row and column indices are zero-based.")]
    public string GetGridItem(
        [Description("The elementId of the grid/table container element")]
        string elementId,
        [Description("Zero-based row index")]
        int row,
        [Description("Zero-based column index")]
        int column)
    {
        try
        {
            var info = _service.GetGridItem(elementId, row, column);
            return ToolResponse.Success(info);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetGridItem));
        }
    }

    [McpServerTool(Name = "get_table_headers"), Description(
        "Gets the row and column headers of a table element. " +
        "The element must support the Table pattern. " +
        "Returns lists of header elements and whether the table is row-major or column-major.")]
    public string GetTableHeaders(
        [Description("The elementId of the table element")]
        string elementId)
    {
        try
        {
            var info = _service.GetTableHeaders(elementId);
            return ToolResponse.Success(info);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetTableHeaders));
        }
    }

    [McpServerTool(Name = "get_parent"), Description(
        "Gets the parent element of a UI element in the control view tree. " +
        "Returns null if the element is at the top of the tree (direct child of the desktop root). " +
        "Useful for navigating up the UI hierarchy.")]
    public string GetParent(
        [Description("The elementId of the element whose parent to retrieve")]
        string elementId)
    {
        try
        {
            var parent = _service.GetParent(elementId);
            if (parent == null)
                return ToolResponse.Success(new { message = "Element is at the top of the control view tree (parent is the desktop root).", parent = (object?)null });

            return ToolResponse.Success(parent);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetParent));
        }
    }
}
