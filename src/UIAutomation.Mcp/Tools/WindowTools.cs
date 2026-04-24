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
}
