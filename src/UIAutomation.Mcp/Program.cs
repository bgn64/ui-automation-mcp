using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UIAutomation.Core;
using UIAutomation.Core.Services;
using UIAutomation.Mcp;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    // Route all logs to stderr so they don't interfere with MCP stdio transport
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Shut down when the host process (VS Code, Copilot CLI, etc.) exits, so we
// don't leave orphaned servers running on Windows when the parent is killed
// abruptly and stdin EOF is never delivered.
builder.Services.AddHostedService<ParentProcessWatchdog>();

// Register Core services
builder.Services.AddSingleton<ElementCache>();
builder.Services.AddSingleton<IUIAutomationService, UIAutomationService>();
builder.Services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();

// Register MCP server with stdio transport and auto-discovered tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
