using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UIAutomation.Core.Services;

namespace UIAutomation.Mcp.Tools;

[McpServerToolType]
public sealed class ScrollTools
{
    private readonly IUIAutomationService _service;
    private readonly ILogger<ScrollTools> _logger;

    public ScrollTools(IUIAutomationService service, ILogger<ScrollTools> logger)
    {
        _service = service;
        _logger = logger;
    }

    [McpServerTool(Name = "scroll"), Description(
        "Scrolls a container element by a relative amount using the Scroll pattern. " +
        "The element must support the Scroll pattern (e.g., a list, tree, or document view). " +
        "Returns the current scroll position after scrolling.\n\n" +
        "At least one of horizontalAmount or verticalAmount must be provided.")]
    public string Scroll(
        [Description("The elementId of the scrollable container")]
        string elementId,
        [Description(
            "Horizontal scroll amount: 'SmallIncrement', 'LargeIncrement', " +
            "'SmallDecrement', 'LargeDecrement'. Omit to not scroll horizontally.")]
        string? horizontalAmount = null,
        [Description(
            "Vertical scroll amount: 'SmallIncrement', 'LargeIncrement', " +
            "'SmallDecrement', 'LargeDecrement'. Omit to not scroll vertically.")]
        string? verticalAmount = null)
    {
        try
        {
            if (string.IsNullOrEmpty(horizontalAmount) && string.IsNullOrEmpty(verticalAmount))
                return ToolResponse.Error("At least one of horizontalAmount or verticalAmount must be provided.");

            var info = _service.Scroll(elementId, horizontalAmount, verticalAmount);
            return ToolResponse.Success(info);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(Scroll));
        }
    }

    [McpServerTool(Name = "set_scroll_percent"), Description(
        "Scrolls a container element to an absolute position (0-100%) using the Scroll pattern. " +
        "The element must support the Scroll pattern. " +
        "Returns the current scroll position after scrolling.\n\n" +
        "At least one of horizontalPercent or verticalPercent must be provided. " +
        "Omitted axes remain unchanged.")]
    public string SetScrollPercent(
        [Description("The elementId of the scrollable container")]
        string elementId,
        [Description("Horizontal scroll position as a percentage (0-100). Omit to keep current position.")]
        double? horizontalPercent = null,
        [Description("Vertical scroll position as a percentage (0-100). Omit to keep current position.")]
        double? verticalPercent = null)
    {
        try
        {
            if (horizontalPercent is null && verticalPercent is null)
                return ToolResponse.Error("At least one of horizontalPercent or verticalPercent must be provided.");

            var info = _service.SetScrollPercent(elementId, horizontalPercent, verticalPercent);
            return ToolResponse.Success(info);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(SetScrollPercent));
        }
    }

    [McpServerTool(Name = "scroll_into_view"), Description(
        "Scrolls a UI element into view within its containing scrollable area. " +
        "The element must support the ScrollItem pattern. " +
        "Use this when an element is offscreen and you need to bring it into view before interacting with it.")]
    public string ScrollIntoView(
        [Description("The elementId of the element to scroll into view")]
        string elementId)
    {
        try
        {
            _service.ScrollIntoView(elementId);
            return ToolResponse.Success(new { message = $"Scrolled element '{elementId}' into view." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(ScrollIntoView));
        }
    }
}
