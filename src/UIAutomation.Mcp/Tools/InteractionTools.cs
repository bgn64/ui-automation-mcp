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

    [McpServerTool(Name = "expand_element"), Description(
        "Expands a UI element such as a combo box, tree node, or menu item. " +
        "The element must support the ExpandCollapse pattern. Returns the new expand/collapse state.")]
    public string ExpandElement(
        [Description("The elementId of the element to expand")]
        string elementId)
    {
        try
        {
            var newState = _service.ExpandElement(elementId);
            return ToolResponse.Success(new { newState });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(ExpandElement));
        }
    }

    [McpServerTool(Name = "collapse_element"), Description(
        "Collapses a UI element such as a combo box, tree node, or menu item. " +
        "The element must support the ExpandCollapse pattern. Returns the new expand/collapse state.")]
    public string CollapseElement(
        [Description("The elementId of the element to collapse")]
        string elementId)
    {
        try
        {
            var newState = _service.CollapseElement(elementId);
            return ToolResponse.Success(new { newState });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(CollapseElement));
        }
    }

    [McpServerTool(Name = "select_element"), Description(
        "Selects a UI element such as a list item, tab item, or radio button. " +
        "The element must support the SelectionItem pattern. " +
        "This performs an exclusive selection (deselects other items in single-select containers).")]
    public string SelectElement(
        [Description("The elementId of the element to select")]
        string elementId)
    {
        try
        {
            _service.SelectElement(elementId);
            return ToolResponse.Success(new { message = $"Selected element '{elementId}'." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(SelectElement));
        }
    }

    [McpServerTool(Name = "deselect_element"), Description(
        "Deselects a UI element, removing it from the current selection. " +
        "The element must support the SelectionItem pattern. " +
        "May fail if the container requires a selection or does not support multi-select.")]
    public string DeselectElement(
        [Description("The elementId of the element to deselect")]
        string elementId)
    {
        try
        {
            _service.DeselectElement(elementId);
            return ToolResponse.Success(new { message = $"Deselected element '{elementId}'." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(DeselectElement));
        }
    }

    [McpServerTool(Name = "get_selection"), Description(
        "Gets the current selection state of a container element such as a list, combo box, or tab control. " +
        "The container must support the Selection pattern. " +
        "Returns the selected items, whether multi-select is allowed, and whether a selection is required.")]
    public string GetSelection(
        [Description("The elementId of the container element to query")]
        string elementId)
    {
        try
        {
            var info = _service.GetSelection(elementId);
            return ToolResponse.Success(info);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetSelection));
        }
    }

    [McpServerTool(Name = "get_focused_element"), Description(
        "Gets the UI element that currently has keyboard focus. " +
        "Returns the element's properties and an elementId for use with other tools.")]
    public string GetFocusedElement()
    {
        try
        {
            var info = _service.GetFocusedElement();
            return ToolResponse.Success(info);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetFocusedElement));
        }
    }

    [McpServerTool(Name = "set_focus"), Description(
        "Sets keyboard focus to a UI element. Use this before send_keys to ensure " +
        "keystrokes go to the correct element.")]
    public string SetFocus(
        [Description("The elementId of the element to focus")]
        string elementId)
    {
        try
        {
            _service.SetFocus(elementId);
            return ToolResponse.Success(new { message = $"Set focus to element '{elementId}'." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(SetFocus));
        }
    }

    [McpServerTool(Name = "send_keys"), Description(
        "Sends keyboard input to a UI element. Sets focus to the element first, " +
        "then sends the specified keystrokes.\n\n" +
        "Format:\n" +
        "- Plain text is typed as-is: \"Hello World\"\n" +
        "- Special keys use braces: {Enter}, {Tab}, {Escape}, {Backspace}, {Delete}, {Space}\n" +
        "- Arrow keys: {Up}, {Down}, {Left}, {Right}\n" +
        "- Navigation: {Home}, {End}, {PageUp}, {PageDown}\n" +
        "- Function keys: {F1} through {F12}\n" +
        "- Modifier combos: {Ctrl+A}, {Ctrl+C}, {Ctrl+V}, {Alt+F4}, {Ctrl+Shift+S}\n" +
        "- Literal braces: {{ for '{' and }} for '}'\n\n" +
        "Examples: \"Hello{Enter}\", \"{Ctrl+A}{Delete}\", \"{Tab}{Tab}value{Enter}\"")]
    public string SendKeys(
        [Description("The elementId of the element to send keys to (will receive focus first)")]
        string elementId,
        [Description("The keys to send, using the format described above")]
        string keys)
    {
        try
        {
            _service.SendKeys(elementId, keys);
            return ToolResponse.Success(new { message = $"Sent keys to element '{elementId}'." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(SendKeys));
        }
    }

    [McpServerTool(Name = "get_range_value"), Description(
        "Gets the range value properties of a UI element such as a slider, spinner, or progress bar. " +
        "Returns current value, minimum, maximum, and step sizes. " +
        "The element must support the RangeValue pattern.")]
    public string GetRangeValue(
        [Description("The elementId of the element to read the range value from")]
        string elementId)
    {
        try
        {
            var info = _service.GetRangeValue(elementId);
            return ToolResponse.Success(info);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetRangeValue));
        }
    }

    [McpServerTool(Name = "set_range_value"), Description(
        "Sets the numeric value of a UI element such as a slider or spinner. " +
        "The element must support the RangeValue pattern and not be read-only. " +
        "Returns the updated range value properties.")]
    public string SetRangeValue(
        [Description("The elementId of the element to set the range value on")]
        string elementId,
        [Description("The numeric value to set (must be within the element's Minimum and Maximum range)")]
        double value)
    {
        try
        {
            var info = _service.SetRangeValue(elementId, value);
            return ToolResponse.Success(info);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(SetRangeValue));
        }
    }

    [McpServerTool(Name = "get_text"), Description(
        "Gets the text content of a UI element using the Text pattern. " +
        "Works with rich text editors, document views, and other controls that support TextPattern. " +
        "For simple text boxes, prefer get_value instead.")]
    public string GetText(
        [Description("The elementId of the element to read text from")]
        string elementId,
        [Description("Maximum number of characters to return. Use -1 for all text (default).")]
        int maxLength = -1)
    {
        try
        {
            var text = _service.GetText(elementId, maxLength);
            return ToolResponse.Success(new { text });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetText));
        }
    }

    [McpServerTool(Name = "move_element"), Description(
        "Moves a UI element to a new screen position. The element must support the Transform pattern " +
        "and allow moving (CanMove must be true). Typically used for windows and floating panels.")]
    public string MoveElement(
        [Description("The elementId of the element to move")]
        string elementId,
        [Description("The new X coordinate (screen pixels)")]
        double x,
        [Description("The new Y coordinate (screen pixels)")]
        double y)
    {
        try
        {
            _service.MoveElement(elementId, x, y);
            return ToolResponse.Success(new { message = $"Moved element '{elementId}' to ({x}, {y})." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(MoveElement));
        }
    }

    [McpServerTool(Name = "resize_element"), Description(
        "Resizes a UI element to the specified dimensions. The element must support the Transform pattern " +
        "and allow resizing (CanResize must be true). Typically used for windows and resizable panels.")]
    public string ResizeElement(
        [Description("The elementId of the element to resize")]
        string elementId,
        [Description("The new width in pixels")]
        double width,
        [Description("The new height in pixels")]
        double height)
    {
        try
        {
            _service.ResizeElement(elementId, width, height);
            return ToolResponse.Success(new { message = $"Resized element '{elementId}' to ({width} x {height})." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(ResizeElement));
        }
    }
}
