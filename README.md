# ui-automation-mcp

An MCP (Model Context Protocol) server that exposes Windows UI Automation as tools — letting AI agents find, inspect, and interact with desktop application UIs.

## What is this for?

This MCP enables **UI automation driven by a large language model**. It gives an AI agent the ability to see, navigate, and operate any Windows desktop application — clicking buttons, filling in forms, reading text, and more — just as a human would.

It is **not intended to replace any first-party MCPs**. Where a dedicated MCP already exists for an application or service, prefer that. This server is meant to **bridge the gaps** where first-party tool support does not yet exist, giving agents a general-purpose fallback for interacting with arbitrary desktop UIs.

> **Tip — skill files for repeat workflows:** The first time an agent navigates a UI through this MCP it must explore and discover elements step-by-step, which can be slow. Once it has successfully completed a workflow, you can have the agent **write a skill file for itself** that encodes the steps it learned. On subsequent runs the agent can replay that skill directly, **significantly improving performance** for the same UI workflow.

## Features

Built with .NET 9 and the Windows UI Automation framework, it provides tools for:

- **Window discovery** — list and manage top-level windows
- **Element search** — find elements by name, automation ID, control type, and more
- **Interaction** — click, type, toggle, select, expand/collapse, scroll, send keystrokes
- **Inspection** — read element properties, values, text content, grid/table data
- **Screen capture** — take screenshots (full screen or per-monitor)
- **Advanced queries** — flexible filtered search with depth control, pattern matching, and property projection

## Installation

### Recommended: install with Scoop

[Scoop](https://scoop.sh/) is a command-line installer for Windows.

```powershell
scoop bucket add ui-automation-mcp https://github.com/bgn64/ui-automation-mcp
scoop install ui-automation-mcp
```

This installs a self-contained `ui-automation-mcp.exe` and adds it to your `PATH` — no .NET SDK or runtime is required.

To upgrade later:

```powershell
scoop update ui-automation-mcp
```

### Add the MCP server to your configuration

Add the following entry to your MCP client's JSON configuration file:

```json
{
  "mcpServers": {
    "ui-automation": {
      "type": "stdio",
      "command": "ui-automation-mcp"
    }
  }
}
```

### Where do I find my MCP configuration file?

The location depends on which MCP client you're using:

| Client | Config file location |
|--------|---------------------|
| **VS Code (GitHub Copilot)** | `.vscode/mcp.json` in your workspace, or `%APPDATA%\Code\User\settings.json` (under `"mcp"` key) |
| **Visual Studio** | `%LOCALAPPDATA%\Microsoft\VisualStudio\<version>\copilot\mcp.json` |
| **Claude Desktop** | `%APPDATA%\Claude\claude_desktop_config.json` |
| **Copilot CLI** | `~/.copilot/config/mcp.json` |
| **Cursor** | `~/.cursor/mcp.json` |

If the file doesn't exist yet, create it with the JSON blob above. If it already exists, merge the `"ui-automation"` entry into the existing `"mcpServers"` object.

After saving the config, restart your MCP client (or reload the window in VS Code). The server will be launched automatically when the client needs it.

## Alternative installation methods

### Manual zip download

If you'd rather not use Scoop, download the latest release zip from the [releases page](https://github.com/bgn64/ui-automation-mcp/releases) and extract it to a permanent folder, e.g. `C:\tools\ui-automation-mcp\`. Then point your MCP client at the absolute path:

```json
{
  "mcpServers": {
    "ui-automation": {
      "type": "stdio",
      "command": "C:\\tools\\ui-automation-mcp\\ui-automation-mcp.exe"
    }
  }
}
```

The zip is self-contained — no .NET SDK or runtime installation is required.

### Run from source

If you have the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed and prefer to run from source, clone the repo and point your MCP config at the project directly:

```json
{
  "mcpServers": {
    "ui-automation": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\ui-automation-mcp\\src\\UIAutomation.Mcp"]
    }
  }
}
```

> **Note:** The first launch will be slower as `dotnet run` compiles the project.

## Requirements

- **Windows** — this server uses the Windows UI Automation API and only runs on Windows.
