using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UIAutomation.Core.Services;

namespace UIAutomation.Mcp.Tools;

[McpServerToolType]
public sealed class InteractionTools
{
    private readonly IUIAutomationService _service;
    private readonly ILogger<InteractionTools> _logger;

    public InteractionTools(IUIAutomationService service, ILogger<InteractionTools> logger)
    {
        _service = service;
        _logger = logger;
    }

    [McpServerTool(Name = "click_element"), Description(
        "Clicks (invokes) a UI element such as a button. The element must support " +
        "the Invoke pattern. Use find_element or get_element_tree to discover clickable elements first.")]
    public string ClickElement(
        [Description("The elementId of the element to click")]
        string elementId)
    {
        try
        {
            _service.InvokeElement(elementId);
            return ToolResponse.Success(new { message = $"Clicked element '{elementId}'." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(ClickElement));
        }
    }

    [McpServerTool(Name = "set_value"), Description(
        "Sets the text value of a UI element such as a text box or edit control. " +
        "The element must support the Value pattern.")]
    public string SetValue(
        [Description("The elementId of the element to set the value on")]
        string elementId,
        [Description("The text value to set")]
        string value)
    {
        try
        {
            _service.SetValue(elementId, value);
            return ToolResponse.Success(new { message = $"Set value on element '{elementId}'." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(SetValue));
        }
    }

    [McpServerTool(Name = "get_value"), Description(
        "Gets the current text value of a UI element. Tries the Value pattern first, " +
        "then falls back to the element's Name property.")]
    public string GetValue(
        [Description("The elementId of the element to read the value from")]
        string elementId)
    {
        try
        {
            var value = _service.GetValue(elementId);
            return ToolResponse.Success(new { value });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetValue));
        }
    }

    [McpServerTool(Name = "toggle_element"), Description(
        "Toggles a UI element such as a checkbox or toggle button. " +
        "The element must support the Toggle pattern. Returns the new toggle state.")]
    public string ToggleElement(
        [Description("The elementId of the element to toggle")]
        string elementId)
    {
        try
        {
            var newState = _service.ToggleElement(elementId);
            return ToolResponse.Success(new { newState });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(ToggleElement));
        }
    }

    [McpServerTool(Name = "click_at_point"), Description(
        "Simulates a physical mouse click at a UI element's clickable point. " +
        "Use this as a fallback when click_element fails because the element does not support " +
        "the Invoke pattern. Works on any visible element by moving the cursor to the element " +
        "and performing a left-click via SendInput.")]
    public string ClickAtPoint(
        [Description("The elementId of the element to click at its clickable point")]
        string elementId)
    {
        try
        {
            _service.ClickAtPoint(elementId);
            return ToolResponse.Success(new { message = $"Clicked at point on element '{elementId}'." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(ClickAtPoint));
        }
    }
}
