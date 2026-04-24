using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UIAutomation.Core.Services;

namespace UIAutomation.Mcp.Tools;

[McpServerToolType]
public sealed class WindowTools
{
    private readonly IUIAutomationService _service;
    private readonly ILogger<WindowTools> _logger;

    public WindowTools(IUIAutomationService service, ILogger<WindowTools> logger)
    {
        _service = service;
        _logger = logger;
    }

    [McpServerTool(Name = "list_windows"), Description(
        "Lists all visible top-level windows on the desktop. " +
        "Returns each window's name, process ID, control type, and an elementId " +
        "that can be used with other tools to interact with that window.")]
    public string ListWindows()
    {
        try
        {
            var windows = _service.ListWindows();
            return ToolResponse.Success(windows);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(ListWindows));
        }
    }

    [McpServerTool(Name = "set_window_state"), Description(
        "Sets the visual state of a window (minimized, maximized, or normal/restored). " +
        "The element must be a window that supports the Window pattern. Returns the new window state.")]
    public string SetWindowState(
        [Description("The elementId of the window")]
        string elementId,
        [Description("The desired window state: 'minimized', 'maximized', or 'normal'")]
        string state)
    {
        try
        {
            var newState = _service.SetWindowVisualState(elementId, state);
            return ToolResponse.Success(new { newState });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(SetWindowState));
        }
    }

    [McpServerTool(Name = "close_window"), Description(
        "Closes a window. The element must be a window that supports the Window pattern. " +
        "WARNING: This is a destructive action that cannot be undone.")]
    public string CloseWindow(
        [Description("The elementId of the window to close")]
        string elementId)
    {
        try
        {
            _service.CloseWindow(elementId);
            return ToolResponse.Success(new { message = $"Closed window '{elementId}'." });
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(CloseWindow));
        }
    }

    [McpServerTool(Name = "get_window_info"), Description(
        "Gets detailed information about a window's state, including visual state " +
        "(minimized/maximized/normal), interaction state, and capabilities " +
        "(can maximize, can minimize, is modal, is topmost). " +
        "The element must be a window that supports the Window pattern.")]
    public string GetWindowInfo(
        [Description("The elementId of the window to inspect")]
        string elementId)
    {
        try
        {
            var info = _service.GetWindowInfo(elementId);
            return ToolResponse.Success(info);
        }
        catch (Exception ex) when (ToolResponse.IsValidationError(ex))
        {
            return ToolResponse.Error(ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResponse.UnexpectedError(_logger, ex, nameof(GetWindowInfo));
        }
    }
}
