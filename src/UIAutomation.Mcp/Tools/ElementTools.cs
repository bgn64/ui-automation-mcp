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
}
