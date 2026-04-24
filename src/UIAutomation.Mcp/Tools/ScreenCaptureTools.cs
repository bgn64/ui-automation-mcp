using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace UIAutomation.Mcp.Tools;

[McpServerToolType]
public sealed class ScreenCaptureTools
{
    private readonly Core.Services.IScreenCaptureService _captureService;
    private readonly ILogger<ScreenCaptureTools> _logger;

    public ScreenCaptureTools(Core.Services.IScreenCaptureService captureService, ILogger<ScreenCaptureTools> logger)
    {
        _captureService = captureService;
        _logger = logger;
    }

    [McpServerTool(Name = "take_screenshot", ReadOnly = true), Description(
        "Captures a screenshot of the entire screen and returns it as a PNG image. " +
        "Use this tool to visually verify the result of UI automation actions.\n\n" +
        "When to use:\n" +
        "- After performing an action with significant or uncertain visual outcome " +
        "(e.g., clicking a button that opens a dialog, navigating to a new page)\n" +
        "- At the end of a multi-step workflow to confirm the final state was achieved\n" +
        "- When element-based inspection alone cannot confirm the intended effect " +
        "(e.g., verifying visual appearance, layout, or content rendering)\n" +
        "- To diagnose unexpected behavior when other tools return ambiguous results\n\n" +
        "The screenshot covers the full virtual screen (all monitors). " +
        "No parameters are required.")]
    public CallToolResult TakeScreenshot(
        [Description("Optional zero-based monitor index. When omitted, captures the full virtual screen (all monitors). " +
                     "When specified, captures only that monitor. Use list_monitors to discover available indices.")]
        int? monitorIndex = null)
    {
        try
        {
            byte[] pngBytes = monitorIndex.HasValue
                ? _captureService.CaptureMonitor(monitorIndex.Value)
                : _captureService.CaptureScreen();

            return new CallToolResult
            {
                Content =
                [
                    ImageContentBlock.FromBytes(pngBytes, "image/png")
                ]
            };
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock { Text = ex.Message }
                ]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in tool '{ToolName}'", nameof(TakeScreenshot));
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock { Text = $"{ex.GetType().Name}: {ex.Message}" }
                ]
            };
        }
    }

    [McpServerTool(Name = "list_monitors", ReadOnly = true), Description(
        "Lists all connected monitors with their indices, device names, bounds (in physical pixels), " +
        "and whether each is the primary display. Use this to discover valid monitorIndex values for take_screenshot.")]
    public CallToolResult ListMonitors()
    {
        try
        {
            var monitors = _captureService.GetMonitors();
            var json = JsonSerializer.Serialize(monitors, SerializerOptions.Default);
            return new CallToolResult
            {
                Content =
                [
                    new TextContentBlock { Text = json }
                ]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in tool '{ToolName}'", nameof(ListMonitors));
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock { Text = $"{ex.GetType().Name}: {ex.Message}" }
                ]
            };
        }
    }
}
